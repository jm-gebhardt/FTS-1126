using Gebhardt.Acc.Common.Domain.Repositories;

namespace Gebhardt.Acc.Statistics.Domain.Repositories;

/// <summary>
/// Provides a repository for heatmap related queries.
/// </summary>
public interface IHeatmapRepository<T> : IRepository<T> where T : class
{
    /// <summary>
    /// Deletes any agv positions older than the given timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp</param>
    Task DeleteAgvPositionsOlderThan(DateTime timestamp);
}