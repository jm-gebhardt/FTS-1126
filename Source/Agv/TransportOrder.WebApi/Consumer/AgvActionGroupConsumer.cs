using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes action group related events and sends messages via <see cref="IAgvPublisher"/>.
/// </summary>
[PublicAPI]
public class AgvActionGroupConsumer : IConsumer<ActionGroupStarted>,
    IConsumer<ActionGroupCompleted>
{
    private readonly ILogger<AgvActionGroupConsumer> _logger;
    private readonly IAgvPublisher _agvPublisher;

    /// <summary>
    /// Initializes a new <see cref="AgvActionGroupConsumer"/>.
    /// </summary>
    public AgvActionGroupConsumer(
        IAgvPublisher agvPublisher,
        ILogger<AgvActionGroupConsumer> logger)
    {
        _agvPublisher = agvPublisher;
        _logger = logger;
    }

    /// <summary>
    /// Send message to all clients.
    /// </summary>
    public Task Consume(ConsumeContext<ActionGroupStarted> context)
    {
        var (actionGroup, correlationId, _) = context.Message;
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(ActionGroupStarted), correlationId);
        return _agvPublisher.SendAssignedActionGroupChangedAsync(actionGroup.AssignedAgv!.Value, actionGroup.CorrelationId);
    }

    /// <summary>
    /// Sends an empty route to all clients when an action group completes.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCompleted> context)
    {
        if (context.Message.ActionGroup.AssignedAgv == null)
        {
            return;
        }

        var agvId = context.Message.ActionGroup.AssignedAgv!.Value;
        await _agvPublisher.SendAgvRouteChangedAsync(new CurrentAgvRouteReadModel(agvId, [], []));
    }
}