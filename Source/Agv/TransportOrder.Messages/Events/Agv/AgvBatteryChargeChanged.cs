using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// An event reporting that the battery charge of an agv has changed.
/// </summary>
/// <param name="CorrelationId">The Agv correlation id.</param>
/// <param name="OldBatteryCharge">Battery charge of the agv before the change [%].</param>
/// <param name="NewBatteryCharge">Current battery charge [%].</param>
public record AgvBatteryChargeChanged(Guid CorrelationId, double OldBatteryCharge, double NewBatteryCharge) : CorrelatedBy<Guid>;