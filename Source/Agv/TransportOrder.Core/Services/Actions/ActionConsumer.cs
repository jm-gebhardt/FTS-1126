using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <summary>
/// Consumer for action messages
/// </summary>
[UsedImplicitly]
public class ActionConsumer :
    IConsumer<QueueActionGroup>,
    IConsumer<ActionCompletedEvent>,
    IConsumer<ChargingAgvsCanHandleActionsAgainEvent>,
    IConsumer<AgvBecameIdle>,
    IConsumer<AgvNeedsCharging>
{
    private readonly ILogger<ActionConsumer> _logger;
    private readonly IActionManager _actionManager;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="actionManager"></param>
    public ActionConsumer(ILogger<ActionConsumer> logger, IActionManager actionManager)
    {
        _logger = logger;
        _actionManager = actionManager;
    }

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<AgvBecameIdle> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        _logger.LogDebug("{MessageType} for Agv: {AgvCorrelationId} received", nameof(AgvBecameIdle), agvCorrelationId);
        return _actionManager.AssignExclusivelyIfAgvReadyAsync(agvCorrelationId, cancellationToken: context.CancellationToken);
    }

    /// <summary>
    /// Handle <see cref="ActionGroup"/> incoming from HostOrderStateMachine
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<QueueActionGroup> context)
    {
        _logger.LogInformation("{MessageType} {CorrelationId} received. The ActionGroup is Manual = {IsManual}", nameof(QueueActionGroup), context.Message.CorrelationId, context.Message.ActionGroup.IsManual);
        await _actionManager.AddActionGroupAsync(context.Message.ActionGroup, context.CancellationToken);
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handle <see cref="ActionCompletedEvent"/> from DeviceCommunication
    /// </summary>
    public Task Consume(ConsumeContext<ActionCompletedEvent> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        _logger.LogDebug("{MessageType} for actionId: {ActionCorrelationId} received", nameof(ActionCompletedEvent), actionCorrelationId);
        return _actionManager.CompleteActionAsync(actionCorrelationId, context.CancellationToken);
    }

    /// <summary>
    /// Handle <see cref="ChargingAgvsCanHandleActionsAgainEvent"/> incoming from ChargingService
    /// </summary>
    /// <param name="context"></param>
    public Task Consume(ConsumeContext<ChargingAgvsCanHandleActionsAgainEvent> context)
    {
        _logger.LogDebug("{MessageType} received", nameof(ChargingAgvsCanHandleActionsAgainEvent));
        return _actionManager.CheckChargingAgvs(context.CancellationToken);
    }

    /// <summary>
    /// Assign new action to get to charger soon
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task Consume(ConsumeContext<AgvNeedsCharging> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        _logger.LogDebug("{MessageType} for Agv: {AgvCorrelationId} received", nameof(AgvNeedsCharging), agvCorrelationId);

        return _actionManager.AssignExclusivelyIfAgvReadyAsync(agvCorrelationId, cancellationToken: context.CancellationToken);
    }
}