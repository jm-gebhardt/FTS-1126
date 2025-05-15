using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Provides a handler for action group related events.
/// </summary>
[UsedImplicitly]
public class ActionEventHandler(IActionGroupRepository actionGroupRepository, ILogger<ActionEventHandler> logger) :
    IConsumer<ActionCompleted>,
    IConsumer<StartingActionForAgvFailed>,
    IConsumer<NonChargingActionCompleted>,
    IConsumer<NodesSelectedForAction>,
    IConsumer<ActionAbortionCompleted>,
    IConsumer<ActionCanceled>,
    IConsumer<ActionDurationEstimated>,
    IConsumer<AutomaticActionWithdrawn>,
    IConsumer<ActionGroupResumed>,
    IConsumer<ActionGroupCreated>,
    IConsumer<ActionGroupQueued>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ActionCompleted> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);

        await context.Publish(new CompleteActionOfActionGroup(actionCorrelationId, actionGroup.CorrelationId));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<StartingActionForAgvFailed> context)
    {
        await context.Publish(new HandleAgvActionStartFailure(context.Message.CorrelationId));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<NonChargingActionCompleted> context)
    {
        await context.Publish(new HandleActionCompletion(context.Message.CorrelationId));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<NodesSelectedForAction> context)
    {
        await context.Publish(new EstimateActionGroupDuration(context.Message.Action, context.Message.AssignedAgv, context.Message.Action.ParentId));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ActionDurationEstimated> context)
    {
        await context.Publish(new PrepareAgvForStartingAction(context.Message.Action, context.Message.AgvId));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ActionAbortionCompleted> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        Guid? assignedAgv = await actionGroupRepository.GetAgvAssignedToAction(actionCorrelationId);
        if (assignedAgv == null)
        {
            logger.LogError("Could not complete action abortion for action {Action}, no assigned agv", actionCorrelationId);
            return;
        }
        await context.Publish(new HandleActionCompletion(assignedAgv.Value));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ActionCanceled> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        Guid? assignedAgv = await actionGroupRepository.GetAgvAssignedToAction(actionCorrelationId);
        if (assignedAgv == null)
        {
            logger.LogError("Could not complete action cancellation for action {Action}, no assigned agv", actionCorrelationId);
            return;
        }
        await context.Publish(new HandleActionCompletion(assignedAgv.Value));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AutomaticActionWithdrawn> context)
    {
        await context.Publish(new FreeNode(context.Message.TargetNodeId, context.Message.ReservedOnly));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ActionGroupResumed> context)
    {
        await context.Publish(new SetNormalMode(context.Message.AgvCorrelationId, context.Message.InitiatingUser));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ActionGroupCreated> context)
    {
        (var actionGroup, var actionGroupCorrelationId, Guid? agvCorrelationId) = context.Message;
        if (agvCorrelationId != null)
        {
            await context.Publish(new AgvBecameIdle(agvCorrelationId.Value));
            return;
        }

        if (actionGroup.Actions.Any(a => a.ActionType == ActionType.Pick))
        {
            await context.Publish(new TryUpdateIdleAction(actionGroupCorrelationId));
            return;
        }

        await context.Publish(new TryReactivateParkingAgvs());
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ActionGroupQueued> context)
    {
        (var actionGroup, var actionGroupCorrelationId, Guid? agvCorrelationId) = context.Message;
        if (agvCorrelationId != null)
        {
            await context.Publish(new AgvBecameIdle(agvCorrelationId.Value));
            return;
        }

        if (actionGroup.Actions.Any(a => a.ActionType == ActionType.Pick))
        {
            await context.Publish(new TryUpdateIdleAction(actionGroupCorrelationId));
            return;
        }

        await context.Publish(new TryReactivateParkingAgvs());
    }
}
