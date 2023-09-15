using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using MassTransit;
using Microsoft.Extensions.Logging;
using TransportOrder.Domain.Repositories;
using TransportOrder.Messages.Events.Agv;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="AgvPositionChanged"/> and sends messages via <see cref="IAgvPublisher"/>.
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class AgvPositionChangedConsumer :
    IConsumer<AgvPositionChanged>, 
    IConsumer<AgvLastNodeIdChanged>
{
    private readonly ILogger<AgvPositionChangedConsumer> _logger;
    private readonly IAgvPublisher _agvPublisher;
    private readonly INodeStateRegistry _nodeStateRegistry;
    private readonly IMapper _mapper;

    /// <summary>
    /// </summary>
    public AgvPositionChangedConsumer(
        ILogger<AgvPositionChangedConsumer> logger,
        IAgvPublisher agvPublisher,
        INodeStateRegistry nodeStateRegistry,
        IMapper mapper)
    {
        _logger = logger;
        _agvPublisher = agvPublisher;
        _nodeStateRegistry = nodeStateRegistry;
        _mapper = mapper;
            
    }

    /// <summary>
    /// Send message to all clients
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Consume(ConsumeContext<AgvPositionChanged> context)
    {
        _logger.LogTrace("{MessageType} {AgvId}/{CorrelationId} received", nameof(AgvPositionChanged), context.Message.AgvId, context.Message.CorrelationId);

        var agvPositionStatus = _mapper.Map<AgvPositionStatus>(context.Message);
        await _agvPublisher.SendAgvPositionAsync(agvPositionStatus);
    }

    /// <summary>
    /// Last node Id has changed
    /// UnOccupy previous node and occupy new node
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task Consume(ConsumeContext<AgvLastNodeIdChanged> context)
    {
        if (context.Message.OldLastNodeId != null)
        {
            await _nodeStateRegistry.UnOccupyNodeAsync(context.Message.CorrelationId);
        }
            
        if (context.Message.NewLastNodeId != null && await _nodeStateRegistry.IsNodeReservedAsync(context.Message.NewLastNodeId))
        {
            await _nodeStateRegistry.OccupyNodeAsync(context.Message.NewLastNodeId, context.Message.CorrelationId);
        }
    }
}