using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvPositionStatus = Gebhardt.StoreWare.Agv.TransportOrder.Core.Models.AgvPositionStatus;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="AgvPositionChanged"/> and sends messages to <see cref="IAgvHubContext"/>.
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class AgvPositionChangedConsumer :
    IConsumer<AgvPositionChanged>, 
    IConsumer<AgvLastNodeIdChanged>
{
    private readonly ILogger<AgvPositionChangedConsumer> _logger;
    private readonly IAgvHubContext _agvHubContext;
    private readonly INodeStateRegistry _nodeStateRegistry;
    private readonly IMapper _mapper;

    /// <summary>
    /// </summary>
    public AgvPositionChangedConsumer(
        ILogger<AgvPositionChangedConsumer> logger,
        IAgvHubContext agvHubContext,
        INodeStateRegistry nodeStateRegistry,
        IMapper mapper)
    {
        _logger = logger;
        _agvHubContext = agvHubContext;
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

        await _agvHubContext.SendAgvPositionToAllClients(agvPositionStatus);
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