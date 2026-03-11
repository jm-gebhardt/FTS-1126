using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.OrderTranslator.Domain.Extensions;
using Gebhardt.Acc.OrderTranslator.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.OrderTranslator.Services.EventHandler;

/// <summary>
/// Handles reporting to the conveyors 
/// </summary>
[UsedImplicitly]
public class ConveyorReportingEventHandler(
    ILogger<ConveyorReportingEventHandler> logger,
    IHostOrderRepository hostOrderRepository,
    IStationRepository stationRepository) :
    IConsumer<DroppingAgvPassedDoppelspielTriggerPoint>,
    IConsumer<PickingAgvPassedDoppelspielTriggerPoint>,
    IConsumer<AgvStartedPick>,
    IConsumer<ManualActionPassedDoppelspielTrigger>,
    IConsumer<DropActionCompleted>
{
    /// <summary>
    /// Handles the <see cref="DroppingAgvPassedDoppelspielTriggerPoint"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<DroppingAgvPassedDoppelspielTriggerPoint> context)
    {
        (var actionDropTarget, var agvCorrelationId, var droppingHostOrderCorrelationId, Guid? nextPickHostOrderCorrelationId) = context.Message;

        var targetStation = await stationRepository.GetStationByName(actionDropTarget.Name);
        if ( targetStation is { NeedsPalletReporting: false } or null)
        {
            return;
        }
        var droppingHostOrder = await hostOrderRepository.GetByIdAsync(droppingHostOrderCorrelationId);
        string dropTarget = droppingHostOrder.Actions.Single(a => a.ActionType == ActionType.Drop).Node;
        string? pickTarget = null;
        if (nextPickHostOrderCorrelationId != null)
        {
            var pickingHostOrder = await hostOrderRepository.GetByIdAsync(nextPickHostOrderCorrelationId.Value);
            pickTarget = pickingHostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
            logger.LogInformation("Reporting order {Order} of agv {AgvCorrelationId} for picking at target {DropStation}/{Target} after dropping order {DropOrder}, at {Time}", pickingHostOrder.OrderId, agvCorrelationId, targetStation.Name, pickTarget, droppingHostOrder.OrderId, DateTime.UtcNow);
        }
        else
        {
            logger.LogInformation("Reporting order {Order} of agv {AgvCorrelationId} for dropping at {DropStation}/{DropTarget} at {Time}", droppingHostOrder.OrderId, agvCorrelationId, targetStation, dropTarget, DateTime.UtcNow);
        }

        await context.Publish(new OrderReportingRequested(droppingHostOrderCorrelationId, targetStation.Name,  dropTarget, pickTarget, droppingHostOrder.Hu, nextPickHostOrderCorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="PickingAgvPassedDoppelspielTriggerPoint"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<PickingAgvPassedDoppelspielTriggerPoint> context)
    {
        var (actionPickTarget, agvCorrelationId, hostOrderCorrelationId) = context.Message;
        var targetStation = await stationRepository.GetStationByName(actionPickTarget.Name);
        if (targetStation is { NeedsPalletReporting: false } or null)
        {
            return;
        }
        var pickingHostOrder = await hostOrderRepository.GetByIdAsync(hostOrderCorrelationId);
        string pickTarget = pickingHostOrder.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
        logger.LogInformation("Reporting order {Order}/{OrderCorrelationId} of agv {AgvCorrelationId} for picking at target {TargetStation}/{Target} at {Time}", pickingHostOrder.OrderId, hostOrderCorrelationId, agvCorrelationId, targetStation.Name, pickTarget, DateTime.UtcNow);
        await context.Publish(new OrderReportingRequested(hostOrderCorrelationId, targetStation.Name, null, pickTarget, null, null));
    }

    /// <summary>
    /// Handles the <see cref="AgvStartedPick"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvStartedPick> context)
    {
        (string targetStationName, var orderId) = context.Message;

        var targetStation = await stationRepository.GetStationByName(targetStationName);
        if (targetStation is { NeedsPalletReporting: false } or null)
        {
            return;
        }
        try
        {
            var pendingPickOrder = await hostOrderRepository.GetByIdAsync(orderId);
            string pickStation = pendingPickOrder.Actions.Single(a => a.ActionType == ActionType.Pick).Node;
            logger.LogInformation("Late reporting of order {Order}/{OrderCorrelationId} for picking at target {Target} at {Time}", pendingPickOrder.OrderId, pendingPickOrder.CorrelationId, pickStation, DateTime.UtcNow);
            await context.Publish(new OrderReportingRequested(orderId, targetStationName, null, pickStation, null, null));
        }
        catch (InvalidOperationException)
        {
            logger.LogWarning("Not requesting report of order at station {StationName} for order {OrderCorrelationId}, host order, thus Sap is unknown", targetStationName, orderId);
        }
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<ManualActionPassedDoppelspielTrigger> context)
    {
        (string sap, string targetNodeName, bool isPicking, string hu, var orderId, var agvCorrelationId) = context.Message;
        var targetStation = await stationRepository.GetStationByName(targetNodeName);
        if (targetStation is { NeedsPalletReporting: false } or null)
        {
            return;
        }
        if (isPicking)
        {
            logger.LogInformation("Reporting order {OrderCorrelationId} of agv {AgvCorrelationId} for picking at target {TargetStation}/{Target} at {Time}", orderId, agvCorrelationId, sap, targetStation.Name, DateTime.UtcNow);
            await context.Publish(new OrderReportingRequested(orderId, targetStation.Name, null, sap, null, null));
            return;
        }
        logger.LogInformation("Reporting order {OrderCorrelationId} of agv {AgvCorrelationId} for dropping at target {TargetStation}/{Target} at {Time}", orderId, agvCorrelationId, sap, targetStation, DateTime.UtcNow);
        await context.Publish(new OrderReportingRequested(orderId, targetStation.Name, sap, null, hu, null));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<DropActionCompleted> context)
    {
        var (correlationId, actionCorrelationId, actionGroup, _) = context.Message;
        var action = actionGroup.Actions.Single(a => a.CorrelationId == actionCorrelationId);
        logger.LogInformation("Agv {AgvCorrelationId} finished {ActionType} {Action} of action group {ActionGroupCorrelationId}", actionGroup.AssignedAgv, action.ActionType, action.CorrelationId, actionGroup.CorrelationId);
        var targetNode = action.Target.Node;
        var targetStation = await stationRepository.GetStationByName(targetNode!.Name);
        if (targetStation is { NeedsPalletReporting: false } or null)
        {
            return;
        }
        if (actionGroup.IsManual)
        {
            string? sap = action.GetSap();
            if (sap == null)
            {
                logger.LogError("Sap not given on manual drop {ActionCorrelationId}, can not send tord", action.CorrelationId);
                return;
            }
            if (actionGroup.HandlingUnit == null)
            {
                logger.LogError("Hu not given for manual drop {ActionCorrelationId}, can not send tord", action.CorrelationId);
                return;
            }
            logger.LogInformation("Requesting report of pallet drop at station {StationName} to conveyor {Sap} for order {OrderCorrelationId}", action.Target.Node!.Name, sap, actionGroup.CorrelationId);
            await context.Publish(new PalletDropReportingRequested(actionGroup.CorrelationId, action.Target.Node!.Name, sap, actionGroup.HandlingUnit));
            return;
        }
        try
        {
            var droppingHostOrder = await hostOrderRepository.GetByIdAsync(correlationId);
            string dropTarget = droppingHostOrder.Target;
            logger.LogInformation("Requesting report of pallet drop at station {StationName} to conveyor {Sap} for order {OrderCorrelationId}", action.Target.Node!.Name, dropTarget, actionGroup.CorrelationId);
            await context.Publish(new PalletDropReportingRequested(actionGroup.CorrelationId, action.Target.Node!.Name, dropTarget, droppingHostOrder.Hu));
        }
        catch(InvalidOperationException)
        {
            logger.LogWarning("Not requesting report of pallet drop at station {StationName} for order {OrderCorrelationId}, host order, thus Sap is unknown", action.Target.Node!.Name, actionGroup.CorrelationId);
        }
    }
}