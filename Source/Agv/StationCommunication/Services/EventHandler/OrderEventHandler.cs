using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.StationCommunication.Messages;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.StationCommunication.Services.EventHandler;

/// <summary>
/// Handles order-related requests.
/// </summary>
[UsedImplicitly]
public class OrderEventHandler :
    IConsumer<OrderReportingRequested>,
    IConsumer<CargoReportingRequested>,
    IConsumer<PalletDropReportingRequested>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<OrderReportingRequested> context)
    {
        (var correlationId, string targetStation, string? dropTarget, string? pickTarget, string? droppedHu, var pickOrder) = context.Message;
        await context.Publish(new ReportOrder(correlationId, targetStation, dropTarget, pickTarget, droppedHu, pickOrder));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<CargoReportingRequested> context)
    {
        (var correlationId, bool hasCargo) = context.Message;
        await context.Publish(new ReportCargo(correlationId, hasCargo));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<PalletDropReportingRequested> context)
    {
        (var correlationId, string stationName, string conveyorName, string handlingUnit) = context.Message;
        await context.Publish(new ReportPalletDrop(correlationId, stationName, conveyorName, handlingUnit));
    }
}