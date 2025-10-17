using JetBrains.Annotations;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;

/// <summary>
/// The 'WE' target station of the short loop.
/// </summary>
public class ShortLoopWeTargetStation
{
    /// <summary>
    /// The id of the system.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The primary target.
    /// </summary>
    public string Primary { get; init; }

    /// <summary>
    /// The locked stations.
    /// </summary>
    public HashSet<string> LockedStations { get; } = [];

    /// <summary>
    /// The current target.
    /// </summary>
    public string Current { get; private set; }

    /// <summary>
    /// True, if the current target station is locked.
    /// </summary>
    public bool IsLocked => LockedStations.Contains(Current);

    /// <inheritdoc cref="ShortLoopWeTargetStation"/>>
    [PublicAPI]
    public ShortLoopWeTargetStation(Guid id, string primary, string current)
    {
        Id = id;
        Primary = primary;
        Current = current;
    }

    /// <inheritdoc cref="ShortLoopWeTargetStation"/>>
    public ShortLoopWeTargetStation(string primary, string current)
    {
        Id = Guid.NewGuid();
        Primary = primary;
        Current = current;
    }

    /// <summary>
    /// Reset to primary target.
    /// </summary>
    public void Reset()
    {
        Current = Primary;
    }

    /// <summary>
    /// Use the next available station.
    /// </summary>
    /// <param name="graphConfig">The graph configuration.</param>
    public void UseNextAvailable(KaeserGraphConfiguration graphConfig)
    {
        string[] possibleTargets = graphConfig.ConveyorStationMapping.OrderByDescending<KeyValuePair<string, string>, string>(c => c.Value).Select(c => c.Key).ToArray();
        Current = possibleTargets.First(s => !LockedStations.Contains(s));
    }
}