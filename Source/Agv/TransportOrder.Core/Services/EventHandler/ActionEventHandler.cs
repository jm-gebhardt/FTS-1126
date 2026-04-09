using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
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
public class ActionEventHandler(IActionGroupRepository actionGroupRepository, IAgvDb agvDb, ILogger<ActionEventHandler> logger) :
    IConsumer<ActionCompletedByAgv>,
    IConsumer<StartingActionForAgvFailed>,
    IConsumer<NonChargingActionCompleted>,
    IConsumer<NodesSelectedForAction>,
    IConsumer<ActionAbortionCompletedByAgv>,
    IConsumer<ActionAbortedByAgv>,
    IConsumer<ActionDurationEstimated>,
    IConsumer<AutomaticActionWithdrawn>,
    IConsumer<ActionGroupResumed>,
    IConsumer<ActionGroupCreated>,
    IConsumer<ActionGroupQueued>,
    IConsumer<ActionFailedByAgv>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ActionCompletedByAgv> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);
        var action = actionGroup.Actions.Single(a => a.CorrelationId == actionCorrelationId);
        if (action.CompletedDate != null)
        {
            logger.LogWarning("Action {Action} of group {ActionGroupCorrelationId} is in state {ActionStation}, not triggering completion ", actionCorrelationId, actionGroup.CorrelationId, action.State);
            return;
        }

        if (actionGroup.AssignedAgv != context.Message.AgvCorrelationId)
        {
            logger.LogWarning("Action group {ActionGroupCorrelationId} was reassigned to agv {AgvCorrelationId}, not triggering completion of action {Action} requested by {Agv}", actionGroup.CorrelationId, actionGroup.AssignedAgv, actionCorrelationId, context.Message.AgvCorrelationId);
            return;
        }

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
        await context.Publish(new HandleActionCompletion(context.Message.CorrelationId, context.Message.IsQueueing));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<NodesSelectedForAction> context)
    {
        var agvId = context.Message.AssignedAgv;
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByActionIdAsync(context.Message.Action.CorrelationId);
        if (actionGroup.AssignedAgv.GetValueOrDefault() != agvId)
        {
            logger.LogDebug("Action group {ActionGroup} is already assigned to another agv {AgvCorrelationId}/{OtherAgv}", actionGroup.CorrelationId, actionGroup.AssignedAgv, agvId);
            return;
        }

        if (actionGroup.EstimatedDuration == null)
        {
            await context.Publish(new EstimateActionGroupDuration(context.Message.Action, context.Message.AssignedAgv, context.Message.Action.ParentId));
        }
        else
        {
            await context.Publish(new PrepareAgvForStartingAction(context.Message.Action, agvId));
        }
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ActionDurationEstimated> context)
    {
        await context.Publish(new PrepareAgvForStartingAction(context.Message.Action, context.Message.AgvId));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ActionAbortionCompletedByAgv> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        Guid? assignedAgv = await actionGroupRepository.GetAgvAssignedToAction(actionCorrelationId);
        if (assignedAgv == null)
        {
            logger.LogError("Could not complete action abortion for action {Action}, no assigned agv", actionCorrelationId);
            return;
        }
        if (assignedAgv != context.Message.AgvCorrelationId)
        {
            logger.LogWarning("Action group was reassigned to agv {AgvCorrelationId}, not triggering completion of action {Action} abortion requested by {Agv}", assignedAgv, actionCorrelationId, context.Message.AgvCorrelationId);
            return;
        }
        await context.Publish(new HandleActionCompletion(assignedAgv.Value));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ActionAbortedByAgv> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);
        var action = actionGroup.Actions.Single(a => a.CorrelationId == actionCorrelationId);
        Guid? agv = actionGroup.AssignedAgv;
        if (action.State is ActionState.Completed or ActionState.Canceled)
        {
            logger.LogInformation("Could not complete action cancellation for action {Action}, it is already in state {State}", actionCorrelationId, action.State);
            return;
        }
        if (agv == null)
        {
            logger.LogError("Could not complete action cancellation for action {Action}, no assigned agv", actionCorrelationId);
            return;
        }
        if (agv != context.Message.AgvCorrelationId)
        {
            logger.LogWarning("Action group was reassigned to agv {AgvCorrelationId}, not triggering completion of action {Action} cancellation requested by {Agv}", agv, actionCorrelationId, context.Message.AgvCorrelationId);
            return;
        }
        if (action.State is ActionState.Aborted)
        {
            await context.Publish(new HandleActionAbortion(agv.Value));
            return;
        }
        await context.Publish(new HandleActionCompletion(agv.Value));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AutomaticActionWithdrawn> context)
    {
        (Guid? agvCorrelationId, var targetNode, bool reservedOnly) = context.Message;
        if (agvCorrelationId != null)
        {
            var agv = await agvDb.Agvs.GetAgvAsync(agvCorrelationId.Value);
            if (agv.AgvStatus?.LastNodeId == targetNode.Id)
            {
                logger.LogInformation("Automatic action withdrawn: not calling free node for agv {AgvCorrelation} already occupying node {Node}", agvCorrelationId, targetNode.Name);
                return;
            }
        }
        logger.LogInformation("Automatic action withdrawn: calling free node for agv {AgvCorrelation} and node {Node}", agvCorrelationId, targetNode);
        await context.Publish(new FreeNode(targetNode.Id, reservedOnly));
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
            ICollection<Agv> infiniteChargingAgvs = await agvDb.Agvs.SelectInfinitelyChargingAgvsWithAboveMinChargeUpBatteryLevel([agvCorrelationId.Value]);
            if (infiniteChargingAgvs.Count > 0 && actionGroup.IsQueued)
            {
                logger.LogInformation("New queued action group {ActionGroupCorrelationId} was created for infinitely charging agv {AgvCorrelationId} with battery level above minimum charge up level, aborting charge action", actionGroup.CorrelationId, actionGroup.AssignedAgv);
                await context.Publish(new AbortChargeAction(agvCorrelationId.Value));
            }
            await context.Publish(new AgvBecameIdle(agvCorrelationId.Value));
            return;
        }

        if (actionGroup.Actions.Any(a => a.ActionType == ActionType.Pick) && actionGroup.IsQueued)
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

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ActionFailedByAgv> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);
        await context.Publish(new HandleActionFailure(actionGroup.CorrelationId, actionCorrelationId));
    }
}