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
    KaeserGraphConfiguration configuration, IOrderRepository orderRepository) :
    IConsumer<DroppingAgvPassedDoppelspielTriggerPoint>,
    IConsumer<PickingAgvPassedDoppelspielTriggerPoint>,
    IConsumer<AgvStartedPick>,
    IConsumer<AgvFinishedPick>
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
        string? pickTarget = null;
        if (shouldPickNext)
        {
            var pickingHostOrder = await hostOrderRepository.GetByIdAsync(nextPickHostOrderCorrelationId!.Value);
            pickTarget = pickingHostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
            await orderRepository.StorePickOrderReport(pickTarget, pickingHostOrder.CorrelationId);
            logger.LogInformation("Reporting order {Order} of agv {AgvCorrelationId} for picking at target {DropStation}/{Target} after dropping order {DropOrder}, at {Time}", pickingHostOrder.OrderId, agvCorrelationId, targetStation, pickTarget, droppingHostOrder.OrderId, DateTime.UtcNow);
        }
        else
        {
            logger.LogInformation("Reporting order {Order} of agv {AgvCorrelationId} for dropping at {DropStation}/{DropTarget} at {Time}", droppingHostOrder.OrderId, targetStation, dropTarget, agvCorrelationId, DateTime.UtcNow);
        }

        conveyorCommunicator.ReportPalletPickDropStation(targetStation, pickTarget, dropTarget);
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
        logger.LogInformation("Reporting order {Order}/{OrderCorrelationId} of agv {AgvCorrelationId} for picking at target {TargetStation}/{Target} at {Time}", pickingHostOrder.OrderId, hostOrderCorrelationId, agvCorrelationId, targetStation, pickTarget, DateTime.UtcNow);
        conveyorCommunicator.ReportPalletPickDropStation(targetStation, pickTarget, null);
        await orderRepository.StorePickOrderReport(pickTarget, pickingHostOrder.CorrelationId);
    }

    /// <summary>
    /// Handles the <see cref="AgvStartedPick"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvStartedPick> context)
    {
        (string targetStation, var orderId, string pickStation) = context.Message;
        if (!await orderRepository.HasOrderBeenReported(pickStation, orderId))
        {
            conveyorCommunicator.ReportPalletPickDropStation(targetStation, pickStation, null);
            var pendingPickOrder = await hostOrderRepository.GetByIdAsync(orderId);

            logger.LogInformation("Late reporting of order {Order}/{OrderCorrelationId} for picking at target {Target} at {Time}", pendingPickOrder.OrderId, pendingPickOrder.CorrelationId, pickStation, DateTime.UtcNow);
            await orderRepository.StorePickOrderReport(pickStation, orderId);
        }
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
        await orderRepository.RemovePickOrderReport(pickStation, orderId);
    }
}