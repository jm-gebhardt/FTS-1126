using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Graphs.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Map = Gebhardt.StoreWare.Agv.Common.Models.Graphs.Map;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides map-related command handlers.
/// </summary>
[UsedImplicitly]
public class MapCommandHandler :
    IConsumer<SaveMap>,
    IConsumer<SaveMaps>,
    IConsumer<AgvGraphChanged>
{
    private readonly IMapRepository _mapRepository;
    private readonly IMapper _mapper;
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<MapCommandHandler> _logger;

    /// <inheritdoc cref="MapCommandHandler"/>
    public MapCommandHandler(IMapRepository mapRepository,
        IMapper mapper,
        IGraphRepository graphRepository,
        ILogger<MapCommandHandler> logger)
    {
        _mapRepository = mapRepository;
        _mapper = mapper;
        _graphRepository = graphRepository;
        _logger = logger;
    }

    private async Task SaveMapAsync(Map messageMap, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default)
    {
        byte[] mapValue = Convert.FromBase64String(messageMap.ImageData);

        var efNodes = _mapper.Map<List<EfNode>>(messageMap.Nodes);
        var efEdges = _mapper.Map<List<EfEdge>>(messageMap.Edges);

        ICollection<Guid> agvIds = await _graphRepository.GetAgvIdsRelatedToNodesAsync(messageMap.Nodes);

        var map = new Domain.Models.Map.Map(
            messageMap.CorrelationId,
            messageMap.MapId,
            mapValue,
            messageMap.Width,
            messageMap.Height,
            messageMap.Resolution,
            messageMap.XOffset,
            messageMap.YOffset,
            messageMap.ModificationDate
        )
        {
            Edges = efEdges,
            Nodes = efNodes,
            MapAgvs = agvIds.Select(agvId => new MapAgv(messageMap.CorrelationId, null, agvId, null)).ToList()
        };

        if (await _mapRepository.AddOrUpdateAsync(map, cancellationToken))
        {
            _logger.LogInformation("Map {MapId}/{MapCorrelationId} has been added or updated in repository.", messageMap.MapId, messageMap.CorrelationId);
        }
        else
        {
            _logger.LogWarning("Map {MapId}/{MapCorrelationId} was not stored in repository.", messageMap.MapId, messageMap.CorrelationId);
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
}