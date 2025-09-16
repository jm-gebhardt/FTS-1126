namespace Gebhardt.Acc.KaeserSidecar.Domain.Models;

/// <summary>
/// Represents an action handled at the decision point.
/// </summary>
public class DecisionPointAction
{
    /// <summary>
    /// The correlation id of the action.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The correlation id of the agv.
    /// </summary>
    public Guid AgvCorrelationId { get; init; }

    /// <summary>
    /// The target station.
    /// </summary>
    public Guid? Target { get; private set; }

    /// <summary>
    /// The time the agv entered the decision point.
    /// </summary>
    public DateTime EntryTime { get; init; }

    /// <summary>
    /// Station count at action entry.
    /// </summary>
    public int StationCount { get; init; }

    /// <inheritdoc cref="DecisionPointAction"/>>
    public DecisionPointAction(Guid correlationId, Guid agvCorrelationId, Guid? targetStationId, DateTime entryTime, int stationCount)
    {
        CorrelationId = correlationId;
        AgvCorrelationId = agvCorrelationId;
        Target = targetStationId;
        EntryTime = entryTime;
        StationCount = stationCount;
    }

    /// <inheritdoc cref="DecisionPointAction"/>>
    public DecisionPointAction(Guid correlationId, Guid agvCorrelationId, DateTime entryTime, int stationCount)
    {
        CorrelationId = correlationId;
        AgvCorrelationId = agvCorrelationId;
        EntryTime = entryTime;
        StationCount = stationCount;
    }

    /// <summary>
    /// The target of the action.
    /// </summary>
    /// <param name="targetStationId"></param>
    public void SetTarget(Guid targetStationId)
    {
        Target = targetStationId;
    }
}