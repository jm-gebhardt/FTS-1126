using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Drawing;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
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
    IConsumer<SetDrawingLayer>,
    IConsumer<DeleteDrawingLayer>
{
    private readonly IMapRepository _mapRepository;
    private readonly IMapper _mapper;
    private readonly IGraphRepository _graphRepository;
    private readonly IBus _bus;
    private readonly ILogger<MapCommandHandler> _logger;

    /// <inheritdoc cref="MapCommandHandler"/>
    public MapCommandHandler(IMapRepository mapRepository,
        IMapper mapper,
        IGraphRepository graphRepository,
        IBus bus,
        ILogger<MapCommandHandler> logger)
    {
        _mapRepository = mapRepository;
        _mapper = mapper;
        _graphRepository = graphRepository;
        _bus = bus;
        _logger = logger;
    }

    private async Task SaveMapAsync(Map messageMap, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default)
    {
        byte[] mapValue = Convert.FromBase64String(messageMap.ImageData);

        var efNodes = _mapper.Map<List<EfNode>>(messageMap.Nodes);
        var efEdges = _mapper.Map<List<EfEdge>>(messageMap.Edges);

        ICollection<Guid> agvIds = await _graphRepository.GetAgvIdsRelatedToNodesAsync(messageMap.Nodes);

        var mapBackgroundId = Guid.NewGuid();
        var map = new Domain.Models.Map.Map(
            messageMap.CorrelationId,
            messageMap.MapId,
            new MapBackground(mapBackgroundId, messageMap.Resolution, messageMap.XOffset, messageMap.YOffset, messageMap.Width, messageMap.Height, mapValue),
            messageMap.ModificationDate
        )
        {
            Edges = efEdges,
            Nodes = efNodes,
            MapAgvs = agvIds.Select(agvId => new MapAgv(messageMap.CorrelationId, null, agvId, null)).ToList()
        };

        if (await _mapRepository.AddOrUpdateAsync(map, cancellationToken))
        {
            _logger.LogInformation("Map {MapId}/{MapCorrelationId} has been added or updated in repository", messageMap.MapId, messageMap.CorrelationId);
            await publishEndpoint.Publish(new Common.Messages.Maps.MapSaved(messageMap), cancellationToken);
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
    public Task Consume(ConsumeContext<SaveMap> context) => SaveMapAsync(context.Message.Map, context, context.CancellationToken);

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<SaveMaps> context)
    {
        // quick implementation of handling potentially multiple maps. Does only happen once at startup.
        foreach (var map in context.Message.Maps)
        {
            await SaveMapAsync(map, context, context.CancellationToken);
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

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<SetDrawingLayer> context)
    {
        var message = context.Message;
        _logger.LogInformation("Setting drawing layer for map {CorrelationId}", message.CorrelationId);

        var drawingLayer = new MapDrawingLayer(message.CorrelationId, message.Elements);

        await _mapRepository.SetDrawingLayerAsync(message.CorrelationId, drawingLayer);

        _logger.LogInformation("Drawing layer set successfully for map {CorrelationId}", message.CorrelationId);

        await context.RespondAsync(new Success());
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DeleteDrawingLayer> context)
    {
        var correlationId = context.Message.CorrelationId;
        _logger.LogInformation("Deleting drawing layer for map {CorrelationId}", correlationId);

        var deleted = await _mapRepository.DeleteDrawingLayerAsync(correlationId);

        if (deleted)
        {
            _logger.LogInformation("Drawing layer deleted for map {CorrelationId}", correlationId);
        }
        else
        {
            _logger.LogInformation("No drawing layer to delete for map {CorrelationId}", correlationId);
        }

        await context.RespondAsync(new Success());
    }
}