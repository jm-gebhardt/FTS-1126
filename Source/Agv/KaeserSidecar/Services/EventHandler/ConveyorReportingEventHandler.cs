using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using Gebhardt.Acc.KaeserSidecar.Messages;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.EventHandler;

/// <summary>
/// Handles reporting to the conveyors 
/// </summary>
[UsedImplicitly]
public class ConveyorReportingEventHandler(
    ILogger<ConveyorReportingEventHandler> logger,
    IPrismatCommunicationHostOrderRepository hostOrderRepository,
    IConveyorCommunicator conveyorCommunicator,
    KaeserGraphConfiguration configuration,
    IOrderRepository orderRepository) :
    IConsumer<DroppingAgvPassedDoppelspielTriggerPoint>,
    IConsumer<PickingAgvPassedDoppelspielTriggerPoint>,
    IConsumer<AgvStartedPick>,
    IConsumer<AgvFinishedPick>,
    IConsumer<AgvFinishedDrop>,
    IConsumer<ManualActionPassedDoppelspielTrigger>
{
    /// <summary>
    /// Handles the <see cref="DroppingAgvPassedDoppelspielTriggerPoint"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<DroppingAgvPassedDoppelspielTriggerPoint> context)
    {
        (string actionDropTarget, var agvCorrelationId, var droppingHostOrderCorrelationId, Guid? nextPickHostOrderCorrelationId, bool shouldPickNext, _) = context.Message;
        if (!configuration.ConveyorStationMapping.TryGetValue(actionDropTarget, out string? targetStation))
        {
            return;
        }
        var droppingHostOrder = await hostOrderRepository.GetByIdAsync(droppingHostOrderCorrelationId);
        string dropTarget = droppingHostOrder.Actions.Single(a => a.ActionType == ActionType.Drop).Node;
        if (await orderRepository.HasOrderBeenReported(dropTarget, droppingHostOrder.CorrelationId))
        {
            logger.LogWarning("Order {Order} of agv {AgvCorrelationId} for dropping at {DropStation}/{DropTarget} has already been reported", droppingHostOrder.OrderId, targetStation, dropTarget, agvCorrelationId);
            return;
        }

        await orderRepository.StoreOrderReport(dropTarget, droppingHostOrder.CorrelationId);
        string? pickTarget = null;
        if (shouldPickNext)
        {
            var pickingHostOrder = await hostOrderRepository.GetByIdAsync(nextPickHostOrderCorrelationId!.Value);
            pickTarget = pickingHostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
            await orderRepository.StoreOrderReport(pickTarget, pickingHostOrder.CorrelationId);
            logger.LogInformation("Reporting order {Order} of agv {AgvCorrelationId} for picking at target {DropStation}/{Target} after dropping order {DropOrder}, at {Time}", pickingHostOrder.OrderId, agvCorrelationId, targetStation, pickTarget, droppingHostOrder.OrderId, DateTime.UtcNow);
        }
        else
        {
            logger.LogInformation("Reporting order {Order} of agv {AgvCorrelationId} for dropping at {DropStation}/{DropTarget} at {Time}", droppingHostOrder.OrderId, agvCorrelationId, targetStation, dropTarget, DateTime.UtcNow);
        }

        conveyorCommunicator.ReportPalletPickDropStation(targetStation, pickTarget, dropTarget, droppingHostOrder.Hu);
    }

    /// <summary>
    /// Handles the <see cref="PickingAgvPassedDoppelspielTriggerPoint"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<PickingAgvPassedDoppelspielTriggerPoint> context)
    {
        (string actionPickTarget, var agvCorrelationId, var hostOrderCorrelationId) = context.Message;
        if (!configuration.ConveyorStationMapping.TryGetValue(actionPickTarget, out string? targetStation))
        {
            return;
        }
        var pickingHostOrder = await hostOrderRepository.GetByIdAsync(hostOrderCorrelationId);
        string pickTarget = pickingHostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
        if (await orderRepository.HasOrderBeenReported(pickTarget, pickingHostOrder.CorrelationId))
        {
            logger.LogWarning("Order {Order} of agv {AgvCorrelationId} for picking at {PickStation}/{PickTarget} has already been reported", pickingHostOrder.OrderId, targetStation, pickTarget, agvCorrelationId);
            return;
        }
        logger.LogInformation("Reporting order {Order}/{OrderCorrelationId} of agv {AgvCorrelationId} for picking at target {TargetStation}/{Target} at {Time}", pickingHostOrder.OrderId, hostOrderCorrelationId, agvCorrelationId, targetStation, pickTarget, DateTime.UtcNow);
        conveyorCommunicator.ReportPalletPickDropStation(targetStation, pickTarget, null, null);
        await orderRepository.StoreOrderReport(pickTarget, pickingHostOrder.CorrelationId);
    }

    /// <summary>
    /// Handles the <see cref="AgvStartedPick"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvStartedPick> context)
    {
        (string targetStation, var orderId, string pickStation) = context.Message;

        if (await orderRepository.HasOrderBeenReported(pickStation, orderId))
        {
            logger.LogWarning("Order {Order} for picking at {PickStation}/{PickTarget} has already been reported", orderId, targetStation, pickStation);
            return;
        }

        conveyorCommunicator.ReportPalletPickDropStation(targetStation, pickStation, null, null);
        var pendingPickOrder = await hostOrderRepository.GetByIdAsync(orderId);

        logger.LogInformation("Late reporting of order {Order}/{OrderCorrelationId} for picking at target {Target} at {Time}", pendingPickOrder.OrderId, pendingPickOrder.CorrelationId, pickStation, DateTime.UtcNow);
        await orderRepository.StoreOrderReport(pickStation, orderId);
    }

    /// <summary>
    /// Handles the <see cref="AgvFinishedPick"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvFinishedPick> context)
    {
        (var orderId, string pickStation) = context.Message;
        if (!await orderRepository.HasOrderBeenReported(pickStation, orderId))
        {
            return;
        }
        await orderRepository.RemoveOrderReport(pickStation, orderId);
    }

    /// <summary>
    /// Handles the <see cref="AgvFinishedDrop"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvFinishedDrop> context)
    {
        (var orderId, string dropStation) = context.Message;
        if (!await orderRepository.HasOrderBeenReported(dropStation, orderId))
        {
            return;
        }
        await orderRepository.RemoveOrderReport(dropStation, orderId);
    }

    /// <summary>
    /// Handles the <see cref="ManualActionPassedDoppelspielTrigger"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ManualActionPassedDoppelspielTrigger> context)
    {
        var (sap, actionTarget, isPicking, hu, orderId, agvCorrelationId) = context.Message;
        if (!configuration.ConveyorStationMapping.TryGetValue(actionTarget, out string? targetStation))
        {
            return;
        }
        if (await orderRepository.HasOrderBeenReported(sap, orderId))
        {
            logger.LogWarning("Order {Order} for target at {Station}/{Target} has already been reported", orderId, targetStation, sap);
            return;
        }
        await orderRepository.StoreOrderReport(sap, orderId);
        if (isPicking)
        {
            logger.LogInformation("Reporting order {OrderCorrelationId} of agv {AgvCorrelationId} for picking at target {TargetStation}/{Target} at {Time}", orderId, agvCorrelationId, sap, targetStation, DateTime.UtcNow);
            conveyorCommunicator.ReportPalletPickDropStation(targetStation, sap, null, null);
            return;
        }

        logger.LogInformation("Reporting order {OrderCorrelationId} of agv {AgvCorrelationId} for dropping at target {TargetStation}/{Target} at {Time}", orderId, agvCorrelationId, sap, targetStation, DateTime.UtcNow);
        conveyorCommunicator.ReportPalletPickDropStation(targetStation, null, sap, hu);
    }
}