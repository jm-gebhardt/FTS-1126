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
    /// Enqueue an agv
    /// </summary>
    /// <param name="actionCorrelationId"></param>
    /// <param name="agvCorrelationId"></param>
    Task Enqueue(Guid actionCorrelationId, Guid agvCorrelationId);

    /// <summary>
    /// Dequeue an agv.
    /// </summary>
    Task Dequeue(Guid actionCorrelationId);
}
