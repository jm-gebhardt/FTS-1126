namespace Gebhardt.Acc.KaeserSidecar.Messages.JamState;

/// <summary>
/// Represents the event raised, when a jam state for a station has been received.
/// </summary>
/// <param name="Station">The station.</param>
/// <param name="Status">The status.</param>
/// <param name="TimeStamp">The time stamp of receiving the state.</param>
public record JamStateReceived(string Station, int Status, DateTime TimeStamp);