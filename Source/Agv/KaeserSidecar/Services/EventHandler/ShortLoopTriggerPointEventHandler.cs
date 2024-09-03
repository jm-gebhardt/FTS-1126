using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Messages;
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
public class ShortLoopTriggerPointEventHandler(
    ILogger<ShortLoopTriggerPointEventHandler> logger,
    IPrismatCommunicationHostOrderRepository hostOrderRepository,
    IConveyorCommunicator conveyorCommunicator,
    KaeserGraphConfiguration configuration,
    IAgvSettingsService agvSettingsService,
    IOrderRepository orderRepository) : 
    IConsumer<AgvPassedShortLoopTriggerPoint>,
    IConsumer<AgvStartedShortLoopPick>,
    IConsumer<AgvFinishedShortLoopPick>
{
    /// <summary>
    /// Handles the <see cref="AgvPassedShortLoopTriggerPoint"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPassedShortLoopTriggerPoint> context)
    {
        (var agvData, var agvStatus, var actionGroup, ICollection<Cargo> _, ICollection<AgvError> _) = context.Message;
        (bool shouldReport, string? targetStation, var hostOrder, var action) = await CheckActionGroup(actionGroup);
        if (!shouldReport)
        {
            return;
        }
        bool isDropping = action is { ActionType: ActionType.Drop };
        bool hasLowBatteryLevel = false;
        if (isDropping)
        {
            hasLowBatteryLevel = await agvSettingsService.CheckAgvHasLowBatteryLevel(agvStatus.CorrelationId);
        }
        string? pickTarget = await GetPickStation(hostOrder!, isDropping, hasLowBatteryLevel);
        conveyorCommunicator.ReportPalletPickDropStation(targetStation!, isDropping, pickTarget);
        if (isDropping)
        {
            bool shouldNotPickNext = hasLowBatteryLevel || pickTarget == null;
            await context.Publish(new DroppingAgvPassedShortLoopTriggerPoint(agvData, actionGroup, shouldNotPickNext));
        }
    }

    private async Task<(bool shouldReport, string? targetStation, HostOrder? order, AgvAction? action)> CheckActionGroup(ActionGroup? actionGroup)
    {
        if (actionGroup == null || actionGroup.Actions.All(a => a.ActionType != ActionType.Pick))
        {
            logger.LogWarning("Skip reporting target station to conveyor: Agv without pick drop action group passed short loop trigger point ({ActionGroup})", actionGroup?.CorrelationId);
            return (false, null, null, null);
        }
        var action = actionGroup.Actions.FirstOrDefault(a => a.State == ActionState.Started);
        if (action == null)
        {
            logger.LogWarning("Skip reporting target station to conveyor: Agv without started action passed trigger point. Current action group is: ({ActionGroup})", actionGroup.CorrelationId);
            return (false, null, null, null);
        }
        if (!configuration.ConveyorStationMapping.TryGetValue(action.Target.NodeId!, out string? targetStation))
        {
            logger.LogWarning("Skip reporting target station to conveyor: Target {Target} is not configured station for drop - right away pick sequence", action.Target.NodeId!);
            return (false, null, null, null);
        }
        var hostOrder = await hostOrderRepository.GetByIdAsync(actionGroup.CorrelationId);
        if (!hostOrder.Actions.All(a => configuration.PrismatKnownTargets.Contains(a.Node)))
        {
            logger.LogWarning("Skip reporting target station to conveyor: Action group {CorrelationId} is not for short loop", actionGroup.CorrelationId);
            return (false, null, null, null);
        }
        return (true, targetStation, hostOrder, action);
    }
    
    private async Task<string?> GetPickStation(HostOrder hostOrder, bool isDropping, bool hasLowBatteryLevel)
    {
        string? pickTarget = null;
        
        if (isDropping && hasLowBatteryLevel)
        {
            return pickTarget;
        }

        if (isDropping)
        {
            var pendingPickOrder = await hostOrderRepository.GetNextPickOnShortLoop();
            if (pendingPickOrder != null)
            {
                pickTarget = pendingPickOrder.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
                logger.LogInformation("Reporting order {Order} for picking at target {Target} at {Time}", pendingPickOrder.OrderId, pickTarget, DateTime.UtcNow);
                orderRepository.StorePickOrderReport(pickTarget, pendingPickOrder.CorrelationId);
            }
            return pickTarget;
        }

        pickTarget = hostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
        logger.LogInformation("Reporting order {Order} for picking at target {Target} at {Time}", hostOrder.OrderId, pickTarget, DateTime.UtcNow);
        orderRepository.StorePickOrderReport(pickTarget, hostOrder.CorrelationId);
        return pickTarget;
    }

    /// <summary>
    /// Handles the <see cref="AgvStartedShortLoopPick"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvStartedShortLoopPick> context)
    {
        (string targetStation, var orderId, string pickStation) = context.Message;
        if (!orderRepository.HasOrderBeenReported(pickStation, orderId))
        {
            conveyorCommunicator.ReportPalletPickDropStation(targetStation, false, pickStation);
            var pendingPickOrder = await hostOrderRepository.GetByIdAsync(orderId);

            logger.LogInformation("Reporting order {Order} for picking at target {Target} at {Time}", pendingPickOrder.OrderId, pickStation, DateTime.UtcNow);
            orderRepository.StorePickOrderReport(pickStation, orderId);
        }
    }

    /// <summary>
    /// Handles the <see cref="AgvFinishedShortLoopPick"/> event.
    /// </summary>
    public  Task Consume(ConsumeContext<AgvFinishedShortLoopPick> context)
    {
        (var orderId, string pickStation) = context.Message;
        if (!orderRepository.HasOrderBeenReported(pickStation, orderId))
        {
            return Task.CompletedTask;
        }
        orderRepository.RemovePickOrderReport(pickStation, orderId);
        return Task.CompletedTask;
    }
}