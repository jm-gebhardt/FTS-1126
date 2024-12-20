namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;

/// <summary>
/// Represents a request to unlock a station.
/// </summary>
/// <param name="CorrelationId">The station to unlock.</param>
/// <param name="User">The user that requested the unlocking.</param>
public record TryUnlockStation(Guid CorrelationId, string User);