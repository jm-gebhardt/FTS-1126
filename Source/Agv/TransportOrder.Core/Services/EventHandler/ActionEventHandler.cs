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
    IConsumer<ActionGroupCreated>
{
    /// <summary>
    /// Handle <see cref="ActionCompleted"/> from DeviceCommunication
    /// </summary>
    public async Task Consume(ConsumeContext<ActionCompleted> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);

        await context.Publish(new CompleteActionOfActionGroup(actionCorrelationId, actionGroup.CorrelationId));
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
        await context.Publish(new EstimateActionGroupDuration(context.Message.Action, context.Message.AssignedAgv, context.Message.Action.ParentId));
    }

    /// <inheritdoc cref="ActionDurationEstimated" />
    public async Task Consume(ConsumeContext<ActionDurationEstimated> context)
    {
        await context.Publish(new PrepareAgvForStartingAction(context.Message.Action, context.Message.AgvId));
    }

    /// <summary>
    /// Handles the <see cref="ActionAbortionCompleted"/> event.
    /// </summary>
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

    /// <summary>
    /// Handles the <see cref="ActionCanceled"/> event.
    /// </summary>
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

    /// <summary>
    /// Handles the <see cref="AutomaticActionWithdrawn"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AutomaticActionWithdrawn> context)
    {
        await context.Publish(new FreeNode(context.Message.TargetNodeId));
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupCreated"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCreated> context)
    {
        (var actionGroup, _, Guid? agvCorrelationId) = context.Message;
        if (agvCorrelationId != null)
        {
            await context.Publish(new AgvBecameIdle(agvCorrelationId.Value));
            return;
        }

        string? targetNode = null;
        if (actionGroup.Actions.Any(a => a.ActionType == ActionType.Pick))
        {
            targetNode = actionGroup.Actions.OrderBy(a => a.Sequence).First().Target.NodeId;
        }
        await context.Publish(new TryReactivateParkingAgvs(targetNode));
    }
}