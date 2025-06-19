namespace Gebhardt.Acc.KaeserSidecar.Domain.Services;

/// <summary>
/// A selector for the next target
/// </summary>
public interface ITargetSelector
{
    /// <summary>
    /// Set a new station count
    /// </summary>
    /// <param name="stationCount">Number of considered stations.</param>
    void SetStationCount(int stationCount);

    /// <summary>
    /// Enqueue an agv.
    /// </summary>
    /// <param name="actionCorrelationId">The correlation id of the action that needs target selection.</param>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    Task Enqueue(Guid actionCorrelationId, Guid agvCorrelationId);

    /// <summary>
    /// Dequeue an agv.
    /// </summary>
    Task Dequeue(Guid actionCorrelationId);
}
