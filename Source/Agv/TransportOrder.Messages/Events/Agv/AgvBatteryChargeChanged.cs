using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the battery charge of an agv has changed.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="OldBatteryCharge">Battery charge of the agv before the change.</param>
/// <param name="NewBatteryCharge">Battery charge of the agv after the change.</param>
public record AgvBatteryChargeChanged(Guid CorrelationId, double OldBatteryCharge, double NewBatteryCharge) : CorrelatedBy<Guid>;