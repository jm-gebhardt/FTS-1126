using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

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

        var oldLastNode = await _nodeStateRepository.GetNodeStateAsync(oldLastNodeId!);
        var newLastNode = await _nodeStateRepository.GetNodeStateAsync(newLastNodeId!);
        if (oldLastNode != null && oldLastNode.IsOccupiedByAgv(agvCorrelationId))
        {
            await _nodeStateRepository.UnOccupyNodeAsync(agvCorrelationId);
            await PublishNodeStateChange(context, oldLastNodeId!);
        }

        if (newLastNode != null && newLastNode.IsReservedForAgv(agvCorrelationId))
        {
            await _nodeStateRepository.OccupyNodeAsync(newLastNodeId!, agvCorrelationId);
            await PublishNodeStateChange(context, newLastNodeId!);
        }

        var agvRoute = await _agvRouteRetrievalService.GetAgvRouteAsync(agvCorrelationId, newLastNodeId);
        if (agvRoute != null)
        {
            _logger.LogTrace("Publishing AgvRoute {AgvRoute}", string.Join(",", agvRoute.RemainingNodes));

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