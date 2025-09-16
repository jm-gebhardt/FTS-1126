namespace Gebhardt.Acc.KaeserSidecar.Domain.Models;

/// <summary>
/// The jam state of a station.
/// </summary>
public class JamState
{
    /// <summary>
    /// The station Id.
    /// </summary>
    public Guid StationId { get; init; }

    /// <summary>
    /// The state.
    /// </summary>
    public int State { get; private set; }

    /// <summary>
    /// True, if the station has a pallet jam.
    /// </summary>
    public bool HasPalletJam => State == 1;

    /// <summary>
    /// The time the jam state was set last.
    /// </summary>

    public DateTime LastStateChangeTime { get; private set; }

    /// <inheritdoc cref="JamState"/>>
    public JamState(Guid stationId, int state, DateTime lastStateChangeTime)
    {
        StationId = stationId;
        State = state;
        LastStateChangeTime = lastStateChangeTime;
    }

    /// <summary>
    /// Set the state.
    /// </summary>
    /// <param name="state">The new state.</param>
    /// <param name="timeStamp">The time the state was changed.</param>
    public void SetState(int state, DateTime timeStamp)
    {
        State = state;
        LastStateChangeTime = timeStamp;
    }
}