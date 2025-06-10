namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the event raised when an agvs device-internally reserved stations changed.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Stations">The new stations list.</param>
public record AgvStationsChanged(Guid CorrelationId, ICollection<string> Stations);