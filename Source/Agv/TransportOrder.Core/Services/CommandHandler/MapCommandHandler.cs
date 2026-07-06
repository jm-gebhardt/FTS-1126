using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Maps;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Gebhardt.Acc.Common.Domain.Services;
using SkiaSharp;
using Map = Gebhardt.Acc.Common.Domain.Models.Graphs.Map;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides map-related command handlers.
/// </summary>
[UsedImplicitly]
public class MapCommandHandler :
    IConsumer<SaveMap>,
    IConsumer<SaveMaps>,
    IConsumer<AgvGraphChanged>,
    IConsumer<UpdateMap>,
    IConsumer<SetBackground>,
    IConsumer<DeleteBackground>,
    IConsumer<GetDefaultMap>,
    IConsumer<GenerateDefaultMap>

{
    private readonly IMapRepository _mapRepository;
    private readonly IMapper _mapper;
    private readonly IGraphRepository _graphRepository;
    private readonly IBus _bus;
    private readonly ILogger<MapCommandHandler> _logger;
    private readonly TimeProvider _timeProvider;

    /// <inheritdoc cref="MapCommandHandler"/>
    public MapCommandHandler(IMapRepository mapRepository,
        IMapper mapper,
        IGraphRepository graphRepository,
        IBus bus,
        TimeProvider timeProvider,
        ILogger<MapCommandHandler> logger)
    {
        _mapRepository = mapRepository;
        _mapper = mapper;
        _graphRepository = graphRepository;
        _bus = bus;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Result of a default map update.
    /// </summary>
    /// <param name="UpdatedDomainMap">Updated map.</param>
    /// <param name="ExtendedAgvMap">Extended map.</param>
    private sealed record DefaultMapUpdateResult(
        Domain.Models.Map.Map UpdatedDomainMap,
        Map ExtendedAgvMap);

    private async Task SaveMapAsync(Map messageMap, Guid agvCorrelationId, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default)
    {
        var incomingNodes = _mapper.Map<List<GraphNode>>(messageMap.Nodes);
        var incomingEdges = _mapper.Map<List<GraphEdge>>(messageMap.Edges);

        bool shouldStoreIncoming = false;
        DefaultMapUpdateResult? updateResult = null;

        bool stored = await _mapRepository.UpdateDefaultMapLockedAsync(async (defaultDomainMap, _) =>
        {
            if (defaultDomainMap.BaseBackground is null)
            {
                _logger.LogWarning("Default map has no base background; skipping composite update");
                return null;
            }

            HashSet<Guid> defaultNodeIds = defaultDomainMap.Nodes.Select(n => n.Id).ToHashSet();
            HashSet<Guid> defaultEdgeIds = defaultDomainMap.Edges.Select(e => e.Id).ToHashSet();
            bool defaultContainsAllIncoming = incomingNodes.All(n => defaultNodeIds.Contains(n.Id))
                && incomingEdges.All(e => defaultEdgeIds.Contains(e.Id));
            bool agvInDefault = defaultDomainMap.MapAgvs.Any(a => a.AgvId == agvCorrelationId);

            if (defaultContainsAllIncoming && agvInDefault)
            {
                // Default already fully covers the incoming map and has the AGV: signal caller to store as view.
                shouldStoreIncoming = true;
                return null;
            }

            var baseMap = new Map(
                defaultDomainMap.CorrelationId,
                defaultDomainMap.MapName,
                defaultDomainMap.ModificationDate,
                Convert.ToBase64String(defaultDomainMap.BaseBackground.Image),
                defaultDomainMap.BaseBackground.Width,
                defaultDomainMap.BaseBackground.Height,
                (float)defaultDomainMap.BaseBackground.Resolution,
                (float)defaultDomainMap.BaseBackground.XOffset,
                (float)defaultDomainMap.BaseBackground.YOffset);

            List<GraphNode> newNodes = incomingNodes.Where(n => !defaultNodeIds.Contains(n.Id)).ToList();
            List<GraphEdge> newEdges = incomingEdges.Where(e => !defaultEdgeIds.Contains(e.Id)).ToList();

            List<GraphNode> allNodes = defaultDomainMap.Nodes.Concat(newNodes).ToList();
            List<GraphEdge> allEdges = defaultDomainMap.Edges.Concat(newEdges).ToList();

            // Use the incoming map's own resolution to get its correct world-space extent.
            // Using the default map's resolution here produces a wrong extent (see GetMapBoundaries formula).
            (float incMinX, float incMaxX, float incMinY, float incMaxY) = messageMap.GetMapBoundaries(messageMap.Resolution);

            // Use the coarser of the two resolutions so a large incoming image (coarser resolution,
            // bigger area) does not force a huge pixel count at the default map's finer resolution.
            float outputResolution = Math.Max(messageMap.Resolution, (float)defaultDomainMap.BaseBackground.Resolution);
            var extendedMap = baseMap.Extend(
                new AgvGraph(defaultDomainMap.CorrelationId, allNodes, allEdges),
                _timeProvider,
                (incMinX, incMinY, incMaxX, incMaxY),
                outputResolution);

            byte[] imageBytes = MapCompositor.Composite(baseMap, messageMap, extendedMap, _logger);

            ICollection<Guid> agvIds = await _graphRepository.GetAgvIdsRelatedToNodesAsync(newNodes);

            var updatedDomainMap = new Domain.Models.Map.Map(
                defaultDomainMap.CorrelationId,
                defaultDomainMap.MapName,
                new MapBackground(Guid.NewGuid(), extendedMap.Resolution, extendedMap.XOffset, extendedMap.YOffset, extendedMap.Width, extendedMap.Height, imageBytes),
                extendedMap.ModificationDate)
            {
                Nodes = allNodes,
                Edges = allEdges,
                MapAgvs = agvIds.Distinct().Select(agvId => new MapAgv(defaultDomainMap.CorrelationId, null, agvId, null)).ToList()
            };

            updateResult = new DefaultMapUpdateResult(updatedDomainMap, extendedMap);
            return (updatedDomainMap, messageMap.CorrelationId);
        }, cancellationToken);

        if (shouldStoreIncoming)
        {
            _logger.LogInformation("Storing incoming map for agv {AgvCorrelationId}", agvCorrelationId);
            await StoreIncomingMapAsync(messageMap, incomingNodes, incomingEdges, agvCorrelationId, publishEndpoint, cancellationToken);
            return;
        }

        if (updateResult is null)
        {
            // Lambda returned null because BaseBackground was missing; already logged.
            return;
        }

        if (stored)
        {
            _logger.LogInformation("Default map {DefaultMapCorrelationId} updated with AGV map {AgvMapCorrelationId}",
                updateResult.UpdatedDomainMap.CorrelationId, messageMap.CorrelationId);
            await publishEndpoint.Publish(new MapSaved(updateResult.ExtendedAgvMap), cancellationToken);
            var mapChangedEvent = _mapper.Map<MapChanged>((updateResult.UpdatedDomainMap, true));
            await publishEndpoint.Publish(mapChangedEvent, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Map {MapId}/{MapCorrelationId} was not stored in repository", messageMap.MapId, messageMap.CorrelationId);
            await publishEndpoint.Publish(new GraphMissing(Guid.NewGuid()), cancellationToken);
        }
    }

    private async Task StoreIncomingMapAsync(Map messageMap, List<GraphNode> nodes, List<GraphEdge> edges, Guid agvCorrelationId, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken)
    {
        byte[] mapValue = Convert.FromBase64String(messageMap.ImageData);
        ICollection<Guid> agvIds = await _graphRepository.GetAgvIdsRelatedToNodesAsync(nodes);

        var map = new Domain.Models.Map.Map(
            messageMap.CorrelationId,
            messageMap.MapId,
            new MapBackground(Guid.NewGuid(), messageMap.Resolution, messageMap.XOffset, messageMap.YOffset, messageMap.Width, messageMap.Height, mapValue),
            messageMap.ModificationDate)
        {
            Edges = edges,
            Nodes = nodes,
            MapAgvs = agvIds.Select(agvId => new MapAgv(messageMap.CorrelationId, null, agvId, null)).ToList()
        };

        if (await _mapRepository.AddOrUpdateAsync(map, agvCorrelationId, cancellationToken))
        {
            _logger.LogDebug("Map {MapId}/{MapCorrelationId} has been added or updated in repository", messageMap.MapId, messageMap.CorrelationId);
            await publishEndpoint.Publish(new MapSaved(messageMap), cancellationToken);
            var mapChangedEvent = _mapper.Map<MapChanged>((map, true));
            await publishEndpoint.Publish(mapChangedEvent, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Map {MapId}/{MapCorrelationId} was not stored in repository", messageMap.MapId, messageMap.CorrelationId);
            await publishEndpoint.Publish(new GraphMissing(Guid.NewGuid()), cancellationToken);
        }
    }

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<SaveMap> context) => SaveMapAsync(context.Message.Map, context.Message.AgvCorrelationId, context, context.CancellationToken);

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<SaveMaps> context)
    {
        // quick implementation of handling potentially multiple maps. Does only happen once at startup.
        foreach (var map in context.Message.Maps)
        {
            await SaveMapAsync(map, context.Message.AgvCorrelationId, context, context.CancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AgvGraphChanged> context)
    {
        await _mapRepository.AssignAgvGraphToMapsAsync(context.Message.CorrelationId);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UpdateMap> context)
    {
        var correlationId = context.Message.CorrelationId;
        await _mapRepository.UpdateMapBackgroundParametersAsync(
            correlationId,
            context.Message.Resolution,
            context.Message.XOffset,
            context.Message.YOffset);

        var map = await _mapRepository.GetMapAsync(correlationId);
        var changed = _mapper.Map<MapChanged>((map, false));
        await _bus.Publish(changed);

        await context.RespondAsync(new Success());
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<SetBackground> context)
    {
        var message = context.Message;

        await using var ms = new MemoryStream(message.Image);
        using var bitmap = SKBitmap.Decode(ms);
        if (bitmap is null)
        {
            throw new BitmapDecodingException("Invalid image data provided.");
        }

        int height = bitmap.Height;
        int width = bitmap.Width;
        var mapBackground = _mapper.Map<MapBackground>(message, opts =>
        {
            opts.Items[nameof(MapBackground.Width)] = width;
            opts.Items[nameof(MapBackground.Height)] = height;
        });
        _logger.LogInformation("Setting custom background for map {CorrelationId} with dimensions {Width}x{Height}", mapBackground.CorrelationId, width, height);
        await _mapRepository.SetCustomBackgroundAsync(message.CorrelationId, mapBackground);

        var map = await _mapRepository.GetMapAsync(message.CorrelationId);
        var changed = _mapper.Map<MapChanged>((map, true));
        await _bus.Publish(changed);

        await context.RespondAsync(new Success());
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DeleteBackground> context)
    {
        var correlationId = context.Message.CorrelationId;

        if (await _mapRepository.DeleteCustomBackgroundAsync(correlationId))
        {
            _logger.LogInformation("Deleted custom background for map {CorrelationId}", correlationId);
            var map = await _mapRepository.GetMapAsync(correlationId);
            var changed = _mapper.Map<MapChanged>((map, true));
            await _bus.Publish(changed);
        }
        else
        {
            _logger.LogInformation("No custom background to delete for map {CorrelationId}", correlationId);
        }

        await context.RespondAsync(new Success());
    }

    /// <inheritdoc cref="MapCommandHandler"/>
    public async Task Consume(ConsumeContext<GetDefaultMap> context)
    {
        Domain.Models.Map.Map domainMap;
        try
        {
            domainMap = await _mapRepository.GetMapAsync(Domain.Models.Map.Map.DefaultMapId);
        }
        catch (MapNotFoundException)
        {
            _logger.LogWarning("Default map not found; seeding on demand");
            var seed = Domain.Models.Map.Map.CreateDefaultMap();
            try
            {
                await _mapRepository.AddPureMapAsync(seed);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not seed default map (may have been seeded concurrently)");
            }
            domainMap = await _mapRepository.GetMapAsync(Domain.Models.Map.Map.DefaultMapId);
        }

        var background = domainMap.BaseBackground
            ?? throw new InvalidOperationException($"Default map {domainMap.CorrelationId} has no base background.");
        string image = Convert.ToBase64String(background.Image);
        await context.RespondAsync(new GotDefaultMap(domainMap.CorrelationId,
            new Map(domainMap.CorrelationId,
                domainMap.MapName,
                domainMap.ModificationDate,
                image,
                background.Width,
                background.Height,
                (float)background.Resolution,
                (float)background.XOffset,
                (float)background.YOffset)));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GenerateDefaultMap> context)
    {
        if (_mapRepository.Exists(Domain.Models.Map.Map.DefaultMapId))
        {
            return;
        }
        await _mapRepository.AddPureMapAsync(Domain.Models.Map.Map.CreateDefaultMap());
    }
}
