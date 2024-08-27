using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.EventHandler;

/// <summary>
/// Handles the short loop trigger point.
/// </summary>
[UsedImplicitly]
public class ShortLoopTriggerPointEventHandler(ILogger<ShortLoopTriggerPointEventHandler> logger, IPrismatCommunicationHostOrderRepository hostOrderRepository, IConveyorCommunicator conveyorCommunicator, KaeserGraphConfiguration configuration, IAgvSettingsService agvSettingsService) : IConsumer<AgvPassedShortLoopTriggerPoint>
{
    /// <summary>
    /// Handles the <see cref="AgvPassedShortLoopTriggerPoint"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedShortLoopTriggerPoint> context)
    {
        (var agvData, var agvStatus, var actionGroup, ICollection<Cargo> _, ICollection<AgvError> _) = context.Message;
        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick))
        {
            logger.LogWarning("Skip reporting target station to conveyor: Agv without pick drop action group passed short loop trigger point ({ActionGroup})", actionGroup?.CorrelationId);
            return;
        }
        var action = actionGroup.Actions.FirstOrDefault(a => a.State == ActionState.Started);
        if (action == null)
        {
            logger.LogWarning("Skip reporting target station to conveyor: Agv without started action passed trigger point. Current action group is: ({ActionGroup})", actionGroup.CorrelationId);
            return;
        }
        if (!configuration.ConveyorStationMapping.TryGetValue(action.Target.NodeId!, out string? targetStation))
        {
            logger.LogWarning("Skip reporting target station to conveyor: Target {Target} is not configured station for drop - right away pick sequence", action.Target.NodeId!);
            return;
        }

        bool isDropping = action is { ActionType: ActionType.Drop };
        bool hasLowBatteryLevel = false;
        if (isDropping)
        {
            hasLowBatteryLevel = await agvSettingsService.CheckAgvHasLowBatteryLevel(agvStatus.CorrelationId);
        }
        string? pickTarget = await GetPickStation(actionGroup, isDropping, hasLowBatteryLevel);
        conveyorCommunicator.ReportPalletPickDropStation(targetStation, isDropping, pickTarget);
        if (isDropping)
        {
            bool shouldNotPickNext = hasLowBatteryLevel || pickTarget == null;
            await context.Publish(new DroppingAgvPassedShortLoopTriggerPoint(agvData, actionGroup, shouldNotPickNext));
        }
    }
    
    private async Task<string?> GetPickStation(ActionGroup actionGroup, bool isDropping, bool hasLowBatteryLevel)
    {
        string? pickTarget = null;
        HostOrder? pendingPickOrder;
        
        if (isDropping && hasLowBatteryLevel)
        {
            return pickTarget;
        }

        if (isDropping)
        {
            pendingPickOrder = await hostOrderRepository.GetNextPickOnShortLoop();
            if (pendingPickOrder != null)
            {
                pickTarget = pendingPickOrder.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
            }
            return pickTarget;
        }

        pendingPickOrder = await hostOrderRepository.GetByIdAsync(actionGroup.CorrelationId);
        pickTarget = pendingPickOrder.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
        return pickTarget;
    }
}