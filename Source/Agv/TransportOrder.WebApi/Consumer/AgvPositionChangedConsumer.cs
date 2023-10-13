using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="AgvPositionChanged"/> and sends messages via <see cref="IAgvPublisher"/>.
/// </summary>
[PublicAPI]
public class AgvPositionChangedConsumer :
    IConsumer<AgvPositionChanged>,
    IConsumer<AgvLastNodeIdChanged>
{
    private readonly ILogger<AgvPositionChangedConsumer> _logger;
    private readonly IAgvPublisher _agvPublisher;
    private readonly INodeStateRegistry _nodeStateRegistry;
    private readonly IActionGroupRegistry _actionGroupRegistry;
    private readonly IActionGroupRetrievalService _actionGroupRetrievalService;
    private readonly IAgvRouteRetrievalService _agvRouteRetrievalService;
    private readonly IMapper _mapper;

    /// <summary>
    /// </summary>
    public AgvPositionChangedConsumer(
        ILogger<AgvPositionChangedConsumer> logger,
        IAgvPublisher agvPublisher,
        INodeStateRegistry nodeStateRegistry,
        IActionGroupRegistry actionGroupRegistry,
        IActionGroupRetrievalService actionGroupRetrievalService,
        IAgvRouteRetrievalService agvRouteRetrievalService,
        IMapper mapper)
    {
        _logger = logger;
        _agvPublisher = agvPublisher;
        _nodeStateRegistry = nodeStateRegistry;
        _actionGroupRegistry = actionGroupRegistry;
        _actionGroupRetrievalService = actionGroupRetrievalService;
        _agvRouteRetrievalService = agvRouteRetrievalService;
        _mapper = mapper;
    }

    /// <summary>
    /// Send message to all clients
    /// </summary>
    /// <param name="context"></param>
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
    public async Task Consume(ConsumeContext<AgvLastNodeIdChanged> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        _logger.LogTrace("{MessageType} {AgvId}/{CorrelationId} received", nameof(AgvLastNodeIdChanged), agvCorrelationId, agvCorrelationId);

        if (context.Message.OldLastNodeId != null)
        {
            await _nodeStateRegistry.UnOccupyNodeAsync(agvCorrelationId);
        }

        string? newLastNodeId = context.Message.NewLastNodeId;
        if (newLastNodeId != null && await _nodeStateRegistry.IsNodeReservedAsync(newLastNodeId))
        {
            await _nodeStateRegistry.OccupyNodeAsync(newLastNodeId, agvCorrelationId);
        }

        var agvRoute = await _agvRouteRetrievalService.GetAgvRouteAsync(agvCorrelationId, newLastNodeId);
        if (agvRoute != null)
        {
            await _agvPublisher.SendAgvRouteAsync(agvRoute);
        }
    }
}