using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes action group related event and sends messages via <see cref="IAgvPublisher"/>
/// </summary>
[PublicAPI]
public class AgvActionGroupConsumer : IConsumer<ActionGroupStarted>,
    IConsumer<ActionGroupCompleted>
{
    private readonly ILogger<AgvActionGroupConsumer> _logger;
    private readonly IAgvPublisher _agvPublisher;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="AgvActionGroupConsumer"/>.
    /// </summary>
    public AgvActionGroupConsumer(
        IAgvPublisher agvPublisher,
        IMapper mapper,
        ILogger<AgvActionGroupConsumer> logger)
    {
        _agvPublisher = agvPublisher;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Send message to all clients
    /// </summary>
    /// <param name="context"></param>
    public Task Consume(ConsumeContext<ActionGroupStarted> context)
    {
        var (actionGroup, correlationId, _) = context.Message;
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(ActionGroupStarted), correlationId);
        return _agvPublisher.SendAssignedActionGroupChangedAsync(actionGroup.AssignedAgv!.Value, actionGroup.CorrelationId);
    }

    /// <summary>
    /// Send SendAgvRouteChanged with empty remaining route.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCompleted> context)
    {
        if (context.Message.ActionGroup.AssignedAgv == null)
        {
            return;
        }
        await _agvPublisher.SendAgvRouteChangedAsync(new CurrentAgvRouteReadModel(context.Message.ActionGroup.AssignedAgv!.Value, [], []));
    }
}