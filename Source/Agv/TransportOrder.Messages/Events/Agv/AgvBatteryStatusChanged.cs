using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>Event fired when any battery-related information changed above thresholds. This also includes the battery charge, but is not limited to.</summary>
/// <param name="CorrelationId">The Agv correlation id.</param>
/// <param name="AgvBatteryStatus">Current Battery status after the change.</param>
public record AgvBatteryStatusChanged(Guid CorrelationId, AgvBatteryStatus AgvBatteryStatus) : CorrelatedBy<Guid>;