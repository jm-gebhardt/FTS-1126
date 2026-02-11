using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
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
    private readonly IAgvRouteRetrievalService _agvRouteRetrievalService;
    private readonly IMapper _mapper;

    /// <summary>
    /// </summary>
    public AgvPositionChangedConsumer(
        ILogger<AgvPositionChangedConsumer> logger,
        IAgvPublisher agvPublisher,
        IAgvRouteRetrievalService agvRouteRetrievalService,
        IMapper mapper)
    {
        _logger = logger;
        _agvPublisher = agvPublisher;
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
    /// Send route changed message to all clients. 
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<AgvLastNodeIdChanged> context)
    {
        (var agvCorrelationId, Guid? _, Guid? newLastNodeId) = context.Message;
        _logger.LogTrace("{MessageType} {AgvId}/{CorrelationId} received", nameof(AgvLastNodeIdChanged), agvCorrelationId, agvCorrelationId);

        var agvRoute = await _agvRouteRetrievalService.GetAgvRouteAsync(agvCorrelationId, newLastNodeId);
        if (agvRoute != null)
        {
            _logger.LogTrace("Publishing AgvRoute {AgvRoute}", string.Join(",", agvRoute.RemainingNodes.Select(n => n.Name)));

            await _agvPublisher.SendAgvRouteChangedAsync(agvRoute);
        }
    }
}