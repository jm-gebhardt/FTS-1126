using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Map;

/// <summary>
/// Represents the event raised when a station's online state has changed.
/// </summary>
/// <param name="CorrelationId">The id of the station.</param>
/// <param name="IsOnline">If the station is now online.</param>
public record StationIsOnlineChanged(Guid CorrelationId, bool IsOnline) : CorrelatedBy<Guid>;