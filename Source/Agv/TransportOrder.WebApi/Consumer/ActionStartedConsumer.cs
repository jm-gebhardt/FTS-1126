using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="ActionStarted"/> and sends message via <see cref="IAgvPublisher"/>
/// </summary>
[PublicAPI]
public class ActionStartedConsumer : IConsumer<ActionStarted>
{
    private readonly ILogger<ActionStartedConsumer> _logger;
    private readonly IAgvPublisher _agvPublisher;
    private readonly IMapper _mapper;
    private readonly IAgvRouteRetrievalService _agvRouteRetrievalService;

    /// <inheritdoc cref="ActionStarted"/>
    public ActionStartedConsumer(
        IAgvPublisher agvPublisher,
        IMapper mapper,
        IAgvRouteRetrievalService agvRouteRetrievalService,
        ILogger<ActionStartedConsumer> logger)
    {
        _agvPublisher = agvPublisher;
        _mapper = mapper;
        _agvRouteRetrievalService = agvRouteRetrievalService;
        _logger = logger;
    }

    /// <summary>
    /// Send message to all clients
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<ActionStarted> context)
    {
        var agvRoute = await _agvRouteRetrievalService.GetAgvRouteAsync(context.Message.AgvCorrelationId);
        if (agvRoute != null)
        {
            await _agvPublisher.SendAgvRouteChangedAsync(agvRoute);
        }
    }
}