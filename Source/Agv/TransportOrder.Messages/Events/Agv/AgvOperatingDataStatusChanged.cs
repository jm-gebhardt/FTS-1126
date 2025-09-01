using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>Event fired when any operating data-related information changed above thresholds.</summary>
/// <param name="CorrelationId">The Agv correlation id.</param>
/// <param name="AgvOperatingDataStatus">Current operating data status after the change.</param>
public record AgvOperatingDataStatusChanged(Guid CorrelationId, AgvOperatingDataStatus AgvOperatingDataStatus) : CorrelatedBy<Guid>;