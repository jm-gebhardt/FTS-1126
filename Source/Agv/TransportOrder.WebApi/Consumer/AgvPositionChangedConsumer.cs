using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Map;
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
    private readonly INodeStateRepository _nodeStateRepository;
    private readonly IActionGroupRetrievalService _actionGroupRetrievalService;
    private readonly IAgvRouteRetrievalService _agvRouteRetrievalService;
    private readonly IMapper _mapper;

    /// <summary>
    /// </summary>
    public AgvPositionChangedConsumer(
        ILogger<AgvPositionChangedConsumer> logger,
        IAgvPublisher agvPublisher,
        INodeStateRepository nodeStateRepository,
        IActionGroupRepository actionGroupRepository,
        IActionGroupRetrievalService actionGroupRetrievalService,
        IAgvRouteRetrievalService agvRouteRetrievalService,
        IMapper mapper)
    {
        _logger = logger;
        _agvPublisher = agvPublisher;
        _nodeStateRepository = nodeStateRepository;
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
        _logger.LogTrace("{MessageType} {AgvId}/{CorrelationId} received. Value: {AgvPositionStatus}", nameof(AgvPositionChanged), context.Message.SerialNumber, context.Message.CorrelationId, context.Message);

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
        (var agvCorrelationId, string? oldLastNodeId, string? newLastNodeId) = context.Message;
        _logger.LogTrace("{MessageType} {AgvId}/{CorrelationId} received", nameof(AgvLastNodeIdChanged), agvCorrelationId, agvCorrelationId);

        if (oldLastNodeId != null)
        {
            bool nodeWasOccupied = await _nodeStateRepository.UnOccupyNodeAsync(agvCorrelationId);
            if (nodeWasOccupied)
            {
                await PublishNodeStateChange(context, oldLastNodeId);
            }
        }

        if (newLastNodeId != null && await _nodeStateRepository.IsNodeReservedAsync(newLastNodeId))
        {
            await _nodeStateRepository.OccupyNodeAsync(newLastNodeId, agvCorrelationId);
            await PublishNodeStateChange(context, newLastNodeId);
        }

        var agvRoute = await _agvRouteRetrievalService.GetAgvRouteAsync(agvCorrelationId, newLastNodeId);
        if (agvRoute != null)
        {
            _logger.LogTrace("Publishing AgvRoute {AgvRoute}", string.Join(",", agvRoute.RemainingRoute));

            await _agvPublisher.SendAgvRouteChangedAsync(agvRoute);
        }
    }

    private async Task PublishNodeStateChange(IPublishEndpoint context, string nodeId)
    {
        var nodeState = await _nodeStateRepository.GetNodeStateAsync(nodeId);
        if (nodeState != null)
        {
            await context.Publish(new NodeStateChanged(nodeState));
            return;
        }
        await context.Publish(new NodeStateChanged(new NodeLock(nodeId, NodeLockState.Free, false, null, DateTime.UtcNow)));
    }
}