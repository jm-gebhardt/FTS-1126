using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using Gebhardt.Acc.StationCommunication.Domain.Services;
using Gebhardt.Acc.StationCommunication.Messages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.StationCommunication.Services.CommandHandler;

/// <summary>
/// Handles order-related commands.
/// </summary>
[UsedImplicitly]
public class OrderCommandHandler(ILogger<OrderCommandHandler> logger, IOrderRepository orderRepository, IConveyorCommunicator conveyorCommunicator) :
    IConsumer<ReportOrder>,
    IConsumer<ReportCargo>,
    IConsumer<ReportPalletDrop>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<ReportOrder> context)
    {
        (var correlationId, string targetStation, string? dropTarget, string? pickTarget, string? droppedHu, Guid? pickOrder) = context.Message;
        if (dropTarget == null && pickTarget == null)
        {
            logger.LogError("No pick or drop target for reporting order {Order} given", correlationId);
            return;
        }
        bool isDoppelSpiel = dropTarget != null && pickTarget != null;
        string lane = dropTarget ?? pickTarget!;

        if (await orderRepository.HasOrderBeenReported(lane, correlationId))
        {
            logger.LogWarning("Order {Order} for station {Station} has already been reported", correlationId, lane);
            return;
        }

        await orderRepository.StoreOrderReport(lane, correlationId);
        if (isDoppelSpiel)
        {
            await orderRepository.StoreOrderReport(pickTarget!, pickOrder.GetValueOrDefault());
            logger.LogInformation("Reporting order {Order} for picking at target {Target} on station {Station} after dropping order {DropOrder} for hu {Hu} at {DropTarget}, at {Time}", pickOrder,  pickTarget, targetStation, correlationId, droppedHu, dropTarget, DateTime.UtcNow);
        }
        else
        {
            logger.LogInformation("Reporting order {Order} for target {Target} on station {Station}, dropped handling unit {Hu}, at {Time}", correlationId, lane, targetStation, droppedHu, DateTime.UtcNow);
        }
        conveyorCommunicator.ReportPalletPickDropStation(targetStation, pickTarget, dropTarget, droppedHu);
    }

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<ReportCargo> context)
    {
        (var correlationId, bool hasCargo) = context.Message;

        logger.LogInformation("Reporting cargo state {HasCargo} for order {Order}", hasCargo, correlationId);
        conveyorCommunicator.ReportAplCargoState(hasCargo);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<ReportPalletDrop> context)
    {
        (var correlationId, string stationName, string conveyorName, string handlingUnit) = context.Message;
        logger.LogInformation("Reporting drop of pallet {Hu} for order {Order} at station {Station} for conveyor {Conveyor}", handlingUnit, correlationId, stationName, conveyorName);
        conveyorCommunicator.ReportPalletDrop(stationName, conveyorName, handlingUnit);
        return Task.CompletedTask;
    }
}