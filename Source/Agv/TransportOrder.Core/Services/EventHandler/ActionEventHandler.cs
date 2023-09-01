using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using TransportOrder.Messages.Commands.ActionGroup;
using TransportOrder.Messages.Events.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Provides a handler for action group related events.
/// </summary>
[UsedImplicitly]
public class ActionEventHandler : IConsumer<ActionGroupCreatedForAgv>,
    IConsumer<QueueActionGroup>,
    IConsumer<ActionCompletedEvent>
{
    private readonly ILogger<AgvEventHandler> _logger;
    private readonly IActionManager _actionManager;

    /// <summary>
    /// Initializes a new <see cref="AgvEventHandler"/> instance.
    /// </summary>
    public ActionEventHandler(
        IActionManager actionManager,
        ILogger<AgvEventHandler> logger)
    {
        _logger = logger;
        _actionManager = actionManager;
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupCreatedForAgv"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCreatedForAgv> context)
    {
        var (actionGroup, assignedAgv) = context.Message;
        
        await context.Publish(new StartAction(actionGroup.Actions.First(), assignedAgv));
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
    public async Task Consume(ConsumeContext<ActionCompletedEvent> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        _logger.LogDebug("{MessageType} for actionId: {ActionCorrelationId} received", nameof(ActionCompletedEvent), actionCorrelationId);
        await _actionManager.CompleteActionAsync(actionCorrelationId, context.CancellationToken);
    }
}