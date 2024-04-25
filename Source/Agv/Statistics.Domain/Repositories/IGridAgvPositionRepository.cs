using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;

namespace Gebhardt.Acc.Statistics.Domain.Repositories;

/// <summary>
/// Provides a repository for agv grid position related queries.
/// </summary>
public interface IGridAgvPositionRepository : IRepository<GridAgvPosition>
{
    /// <summary>
    /// Gets the position frequencies for the given grid.
    /// </summary>
    /// <param name="gridId">The CorrelationId or the Grid.</param>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater Timestamp will be considered.</param>
    /// <param name="toDate">The UTC to Date, only items with a less Timestamp will be considered.</param>
    Task<CountHeatmap> GetPositionFrequency(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate);

    /// <summary>
    /// Deletes any agv positions older than the given timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp</param>
    Task DeleteAgvPositionsOlderThan(DateTime timestamp);
    
    /// <summary>
    /// Get the grid position matching parameters. 
    /// </summary>
    /// <param name="gridId">The grid id.</param>
    /// <param name="xPosition">The grid index in x-direction.</param>
    /// <param name="yPosition">The grid index in y-direction.</param>
    /// <param name="z">The z position.</param>
    /// <param name="agvSerialNumber">The agv serial number.</param>
    /// <returns></returns>
    Task<GridAgvPosition> GetPositionAsync(Guid gridId, int xPosition, int yPosition, float z, string agvSerialNumber);
}