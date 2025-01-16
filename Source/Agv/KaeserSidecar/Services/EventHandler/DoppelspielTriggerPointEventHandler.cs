using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
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
    KaeserGraphConfiguration configuration,
    IPrismatCommunicationHostOrderRepository hostOrderRepository,
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
        var action = actionGroup.Actions.FirstOrDefault(a => a.State == ActionState.Started && a.Target.NodeId == targetStationName);
        if (action == null)
        {
            logger.LogDebug("Agv without started action to station with Doppelspiel passed Doppelspiel trigger point. Current action group is: ({ActionGroup})", actionGroup.CorrelationId);
            return ;
        }
        bool isDropping = action is { ActionType: ActionType.Drop };
        var hostOrder = await hostOrderRepository.GetByIdAsync(actionGroup.CorrelationId);
        if (isDropping)
        {
            var nextPickOrder = await hostOrderRepository.GetNextNotQueuedHostOrderPickingAt(configuration.DoppelspielPickTargets[hostOrder.Actions.Single(a => a.ActionType == ActionType.Drop).Node]);
            bool hasLowBatteryLevel = await agvSettingsService.CheckAgvHasLowBatteryLevel(agvStatus.CorrelationId);
            Guid? nextPickOrderId = nextPickOrder?.CorrelationId ?? null;
            bool shouldPickNext = !hasLowBatteryLevel && nextPickOrderId != null;
            logger.LogInformation("Dropping agv {AgvCorrelationId} passed Doppelspiel trigger point and will drop at {Target} and pick next: {ShouldPick}", actionGroup.AssignedAgv!.Value, action.Target.NodeId!, shouldPickNext);
            await context.Publish(new DroppingAgvPassedDoppelspielTriggerPoint(action.Target.NodeId!, actionGroup.AssignedAgv!.Value, hostOrder.CorrelationId, nextPickOrderId, shouldPickNext));
            return;
        }
        logger.LogInformation("Picking agv {AgvCorrelationId} passed Doppelspiel trigger point and will pick at {Target} ", actionGroup.AssignedAgv!.Value, action.Target.NodeId!);
        await context.Publish(new PickingAgvPassedDoppelspielTriggerPoint(action.Target.NodeId!, actionGroup.AssignedAgv!.Value, hostOrder.CorrelationId));
    }
}