using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Provides a handler for action group related events.
/// </summary>
[UsedImplicitly]
public class ActionEventHandler :
    IConsumer<ActionGroupCreatedForAgv>,
    IConsumer<ActionCompleted>,
    IConsumer<StartingActionForAgvFailed>,
    IConsumer<NonChargingActionCompleted>,
    IConsumer<NodesSelectedForAction>
{
    private readonly ILogger<ActionEventHandler> _logger;

    /// <summary>
    /// Initializes a new <see cref="ActionEventHandler"/> instance.
    /// </summary>
    public ActionEventHandler(
        ILogger<ActionEventHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupCreatedForAgv"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCreatedForAgv> context)
    {
        var (actionGroup, assignedAgv) = context.Message;

        await context.Publish(new SelectNodesForAction(actionGroup.Actions.OrderBy(a => a.Sequence).First(), assignedAgv));
    }

    /// <summary>
    /// Handle <see cref="ActionCompleted"/> from DeviceCommunication
    /// </summary>
    public async Task Consume(ConsumeContext<ActionCompleted> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        _logger.LogDebug("{MessageType} for actionId: {ActionCorrelationId} received", nameof(ActionCompleted), actionCorrelationId);
        await context.Publish(new CompleteAction(actionCorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="StartingActionForAgvFailed"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<StartingActionForAgvFailed> context)
    {
        await context.Publish(new HandleAgvActionStartFailure(context.Message.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="NonChargingActionCompleted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<NonChargingActionCompleted> context)
    {
        await context.Publish(new HandleActionCompletion(context.Message.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="NodesSelectedForAction"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<NodesSelectedForAction> context)
    {
        await context.Publish(new PrepareAgvForStartingAction(context.Message.Action, context.Message.AssignedAgv));
    }
}