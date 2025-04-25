using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.EventHandler;

/// <summary>
/// Handles Doppelspiel trigger related events.
/// </summary>
[UsedImplicitly]
public class DoppelspielTriggerPointEventHandler(
    ILogger<DoppelspielTriggerPointEventHandler> logger,
    IPrismatCommunicationHostOrderRepository hostOrderRepository,
    IPrismatCommunicationStationRepository stationRepository,
    IPrismatCommunicationSystemStatusRepository systemStatusRepository,
    IAgvSettingsService agvSettingsService) :
    IConsumer<AgvPassedDoppelspielTriggerPoint>
{
    /// <summary>
    /// Handles the <see cref="AgvPassedDoppelspielTriggerPoint"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedDoppelspielTriggerPoint> context)
    {
        (_, var agvStatus, var actionGroup, ICollection<Cargo> _, ICollection<AgvError> _, string targetStationName) = context.Message;
        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick))
        {
            logger.LogWarning("Agv without pick drop action group passed Doppelspiel trigger point ({ActionGroup})", actionGroup?.CorrelationId);
            return;
        }
        var action = actionGroup.Actions.FirstOrDefault(a => a.State is ActionState.Started or ActionState.Completed && a.Target.NodeId == targetStationName);
        if (action == null)
        {
            logger.LogWarning("Agv without started action to station {TargetStation} with Doppelspiel passed Doppelspiel trigger point. Current action group is: ({ActionGroup})", targetStationName, actionGroup.CorrelationId);
            foreach (var a in actionGroup.Actions)
            {
                logger.LogWarning("Action {ActionType} is in State {State} and heads to {Target}", a.ActionType, a.State, a.Target.NodeId);
            }
            return;
        }

        bool isDropping = action is { ActionType: ActionType.Drop };
        var hostOrder = await hostOrderRepository.GetByIdAsync(actionGroup.CorrelationId);
        if (isDropping)
        {
            var nextPickOrder = hostOrder.DoppelspielPartner != null? await hostOrderRepository.GetByIdAsync(hostOrder.DoppelspielPartner.Value) : null;
            bool hasLowBatteryLevel = await agvSettingsService.CheckAgvHasLowBatteryLevel(agvStatus.CorrelationId);
            Guid? nextPickOrderId = nextPickOrder == null || nextPickOrder.IsQueued ? null : nextPickOrder.CorrelationId;
            bool shouldPickNext = !hasLowBatteryLevel && nextPickOrderId != null && await CanBeUsedForDoppelspiel(nextPickOrder);
            string? pickNode = shouldPickNext ? action.Target.NodeId : null;
            logger.LogInformation("Dropping agv {AgvCorrelationId} passed Doppelspiel trigger point and will drop at {Target} and pick next {ShouldPick} and has low battery level {HasLowBatteryLevel}", actionGroup.AssignedAgv!.Value, action.Target.NodeId!, shouldPickNext, hasLowBatteryLevel);
            await context.Publish(new DroppingAgvPassedDoppelspielTriggerPoint(action.Target.NodeId!, actionGroup.AssignedAgv!.Value, hostOrder.CorrelationId, nextPickOrderId, shouldPickNext, pickNode));
            return;
        }
        logger.LogInformation("Picking agv {AgvCorrelationId} passed Doppelspiel trigger point and will pick at {Target} ", actionGroup.AssignedAgv!.Value, action.Target.NodeId!);
        await context.Publish(new PickingAgvPassedDoppelspielTriggerPoint(action.Target.NodeId!, actionGroup.AssignedAgv!.Value, hostOrder.CorrelationId));
    }

    private async Task<bool> CanBeUsedForDoppelspiel(HostOrder? hostOrder)
    {
        if (hostOrder == null)
        {
            return false;
        }

        if (hostOrder.IsQueued)
        {
            return false;
        }

        var systemStatus = await systemStatusRepository.GetStatus();
        if (systemStatus is not { UseAklAgvLimit: true })
        {
            return true;
        }

        var stationGroup = await stationRepository.GetStationGroup(hostOrder.Source, hostOrder.Target);
        if (stationGroup == null)
        {
            return true;
        }
        if (stationGroup.IsOccupied)
        {
            return false;
        }
        return !await hostOrderRepository.HasCurrentHostOrdersForStationGroup(stationGroup.Sources, stationGroup.Targets);
    }
}