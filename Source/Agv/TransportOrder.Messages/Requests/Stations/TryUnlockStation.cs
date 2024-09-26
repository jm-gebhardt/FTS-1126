namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;

/// <summary>
/// Represents a request to unlock a station.
/// </summary>
/// <param name="CorrelationId">The station to unlock.</param>
public record TryUnlockStation(Guid CorrelationId);