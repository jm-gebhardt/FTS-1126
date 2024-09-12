namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;

/// <summary>
/// Represents a response that is returned when a station has been locked or unlocked
/// </summary>
/// <param name="Result">If the locking/unlocking was successful.</param>
public record LockedUnlockedStation(bool Result);