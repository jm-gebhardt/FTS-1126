namespace Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

/// <summary>
/// Represents an action handled at the decision point.
/// </summary>
public class DecisionPointAction
{
    /// <summary>
    /// The correlation id of the action.
    /// </summary>
    public Guid AgvActionCorrelationId { get; init; }

    /// <summary>
    /// The correlation id of the agv.
    /// </summary>
    public Guid AgvCorrelationId { get; init; }

    /// <summary>
    /// The target station.
    /// </summary>
    public Guid? TargetNodeId { get; private set; }

    /// <summary>
    /// The time the agv entered the decision point.
    /// </summary>
    public DateTime EntryTime { get; init; }

    /// <summary>
    /// Considered stations at action entry.
    /// </summary>
    public ICollection<Guid> ConsideredStations { get; init; }

    /// <inheritdoc cref="DecisionPointAction"/>>
    public DecisionPointAction(Guid agvActionCorrelationId, Guid agvCorrelationId, Guid? targetStationId, DateTime entryTime, ICollection<Guid> consideredStations)
    {
        AgvActionCorrelationId = agvActionCorrelationId;
        AgvCorrelationId = agvCorrelationId;
        TargetNodeId = targetStationId;
        EntryTime = entryTime;
        ConsideredStations = consideredStations;
    }

    /// <inheritdoc cref="DecisionPointAction"/>>
    public DecisionPointAction(Guid agvActionCorrelationId, Guid agvCorrelationId, DateTime entryTime, ICollection<Guid> consideredStations)
    {
        AgvActionCorrelationId = agvActionCorrelationId;
        AgvCorrelationId = agvCorrelationId;
        EntryTime = entryTime;
        ConsideredStations = consideredStations;
    }

    /// <summary>
    /// The target of the action.
    /// </summary>
    /// <param name="targetStationId"></param>
    public void SetTarget(Guid targetStationId)
    {
        TargetNodeId = targetStationId;
    }
}