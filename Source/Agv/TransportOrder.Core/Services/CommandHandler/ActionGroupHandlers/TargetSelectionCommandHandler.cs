using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler.ActionGroupHandlers;

/// <summary>
/// Provides handling for target selection related messages.
/// </summary>
[UsedImplicitly]
public class TargetSelectionCommandHandler(ILogger<TargetSelectionCommandHandler> logger, ITargetSelector selector, IWaitingHandler waitingHandler, IActionGroupRepository actionGroupRepository, ITargetSelector targetSelector) :
    IConsumer<AgvPassedDecisionPoint1>,
    IConsumer<AgvPassedShortLoopWaitingHandler>,
    IConsumer<AgvLeftShortLoopWaitingHandler>,
    IConsumer<PickActionCompleted>, 
    IConsumer<ActionAborted>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AgvPassedDecisionPoint1> context)
    {
        (_, var agvStatus, var actionGroup, ICollection<Cargo> _, ICollection<AgvError> _, ICollection<Guid> consideredStations, bool useJamState) = context.Message;
        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick) || actionGroup.Actions.All(a => a.Target.Node != null))
        {
            logger.LogWarning("Skip target selection: Agv {AgvCorrelationId} passed decision point without incomplete pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup?.CorrelationId);
            return;
        }
        var action = actionGroup.Actions.FirstOrDefault(a => a.Target.Node == null);
        if (action == null)
        {
            logger.LogWarning("Skip target selection: Agv {AgvCorrelationId} passed decision point without incomplete pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup.CorrelationId);

            return;
        }
        selector.SetUseJamState(useJamState);
        selector.SetStationCount(consideredStations);
        await selector.Enqueue(action.CorrelationId, agvStatus.CorrelationId);
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AgvPassedShortLoopWaitingHandler> context)
    {
        (_, var agvStatus, var actionGroup, ICollection<Cargo> _, ICollection<AgvError> _, var primaryTarget, ICollection<Guid>? secondaryTargets) = context.Message;
        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick) || actionGroup.Actions.All(a => a.Target.Node != null))
        {
            logger.LogWarning("Skip waiting order selection: Agv {AgvCorrelationId} passed short loop waiting handler without incomplete pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup?.CorrelationId);
            return;
        }
        var moveAction = actionGroup.Actions.FirstOrDefault(a => a.ActionType == ActionType.Move);
        if (moveAction == null || (moveAction.StartedDate == null && moveAction.Sequence == 1))
        {
            // Prevent order selection before even reaching the waiting point if agv is going to drop at PSX
            logger.LogWarning("Skip waiting order selection: Agv {AgvCorrelationId} passed short loop waiting handler before picking at AKL station ({ActionGroup})", agvStatus.CorrelationId, actionGroup.CorrelationId);

            return;
        }

        var incompleteAction = actionGroup.Actions.FirstOrDefault(a => a.Target.Node == null);
        if (incompleteAction == null)
        {
            logger.LogWarning("Skip waiting order selection: Agv {AgvCorrelationId} passed short loop waiting handler without without incomplete pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup.CorrelationId);

            return;
        }
        await waitingHandler.ConfigureTargets(primaryTarget, secondaryTargets ?? []);

        // Use current time for order of action waiting handling to ensure competition with long loop
        // is according to trigger entry time, i.e. order of arrival
        await waitingHandler.StartHandling(incompleteAction.CorrelationId, agvStatus.CorrelationId, DateTime.UtcNow);
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AgvLeftShortLoopWaitingHandler> context)
    {
        (_, var agvStatus, var actionGroup, ICollection<Cargo> _, ICollection<AgvError> _) = context.Message;

        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick))
        {
            logger.LogWarning("Skip removal of waiting order : Agv {AgvCorrelationId} passed short loop waiting handler without  pick drop action group  ({ActionGroup})", agvStatus.CorrelationId, actionGroup?.CorrelationId);
            return;
        }

        var action = actionGroup.Actions.FirstOrDefault(a => a.State == ActionState.Started || a.Target.StationOptions.Contains(a.Target.Node!.Name));
        if (action == null)
        {
            logger.LogWarning("Skip removal of waiting order : Agv {AgvCorrelationId} passed short loop waiting handler w/o action heading towards short loop we target station or started action ({ActionGroup})", agvStatus.CorrelationId, actionGroup.CorrelationId);

            return;
        }

        await waitingHandler.StopHandling(action.CorrelationId);
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<PickActionCompleted> context)
    {
        var (correlationId, actionCorrelationId, _) = context.Message;
        var actionGroup = await actionGroupRepository.GetActionGroupAsync(correlationId);
        if (actionGroup == null)
        {
            logger.LogWarning("Could not find action {ActionCorrelationId} or group {ActionGroupCorrelationId} to handle action completion", actionCorrelationId, correlationId);
            return;
        }

        var action = actionGroup.Actions.SingleOrDefault(a => a.CorrelationId == actionCorrelationId);
        if (action == null)
        {
            logger.LogWarning("Could not find action {ActionCorrelationId} or group {ActionGroupCorrelationId} to handle action completion", actionCorrelationId, actionGroup.CorrelationId);
            return;
        }

        if (action.ShouldSkip)
        {
            logger.LogWarning("Ignoring completion of skipped action {ActionCorrelationId} or group {ActionGroupCorrelationId} to handle action completion", actionCorrelationId, actionGroup.CorrelationId);
            return;
        }
        if (actionGroup.IsManual)
        {
            return;
        }
        await targetSelector.Dequeue(action.CorrelationId);
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<ActionAborted> context)
    {
        var actionInMessage = context.Message.Action;
        var (correlationId, actionCorrelationId, _) = context.Message;
        var actionGroup = await actionGroupRepository.GetActionGroupAsync(actionInMessage.ParentId);
        if (actionGroup == null)
        {
            logger.LogWarning("Could not find action {ActionCorrelationId} or group {ActionGroupCorrelationId} to handle action completion", actionCorrelationId, correlationId);
            return;
        }

        var action = actionGroup.Actions.SingleOrDefault(a => a.CorrelationId == actionInMessage.CorrelationId);
        if (action == null)
        {
            logger.LogWarning("Could not find action {ActionCorrelationId} or group {ActionGroupCorrelationId} to handle action completion", actionCorrelationId, actionGroup.CorrelationId);
            return;
        }

        if (action.ShouldSkip)
        {
            logger.LogWarning("Ignoring completion of skipped action {ActionCorrelationId} or group {ActionGroupCorrelationId} to handle action completion", actionCorrelationId, actionGroup.CorrelationId);
            return;
        }
        if (actionGroup.IsManual)
        {
            return;
        }
        await targetSelector.Dequeue(action.CorrelationId);
    }
}