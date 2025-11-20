using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="ActionStartedByAgv"/> and sends message via <see cref="IAgvPublisher"/>
/// </summary>
[PublicAPI]
public class ActionStartedConsumer : 
    IConsumer<ActionStartedByAgv>,
    IConsumer<ActionUpdatedByAgv>
{
    private readonly ILogger<ActionStartedConsumer> _logger;
    private readonly IAgvPublisher _agvPublisher;
    private readonly IAgvRouteRetrievalService _agvRouteRetrievalService;

    /// <inheritdoc cref="ActionStartedByAgv"/>
    public ActionStartedConsumer(
        IAgvPublisher agvPublisher,
        IAgvRouteRetrievalService agvRouteRetrievalService,
        ILogger<ActionStartedConsumer> logger)
    {
        _agvPublisher = agvPublisher;
        _agvRouteRetrievalService = agvRouteRetrievalService;
        _logger = logger;
    }

    /// <summary>
    /// Send message to all clients
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<ActionStartedByAgv> context)
    {
        var agvRoute = await _agvRouteRetrievalService.GetAgvRouteAsync(context.Message.AgvCorrelationId);
        if (agvRoute != null)
        {
            await _agvPublisher.SendAgvRouteChangedAsync(agvRoute);
        }
    }

    /// <summary>
    /// Send message to all clients
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<ActionUpdatedByAgv> context)
    {
        var agvRoute = await _agvRouteRetrievalService.GetAgvRouteAsync(context.Message.AgvCorrelationId);
        if (agvRoute != null)
        {
            await _agvPublisher.SendAgvRouteChangedAsync(agvRoute);
        }
    }
}