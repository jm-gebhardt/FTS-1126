using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;

namespace Gebhardt.Acc.Statistics.Domain.Repositories;

/// <summary>
/// Provides a repository for grid cell stay related queries.
/// </summary>
public interface IGridCellStayRepository : IHeatmapRepository<GridCellStay>
{
    /// <summary>
    /// Gets the stay durations for the given grid.
    /// </summary>
    /// <param name="gridId">The CorrelationId or the Grid.</param>
    /// <param name="mapId">The CorrelationId of the Map.</param>
    /// <param name="fromDate">The UTC from Date, only items with a greater Timestamp will be considered.</param>
    /// <param name="toDate">The UTC to Date, only items with a less Timestamp will be considered.</param>
    /// <param name="agvCorrelationIds"></param>
    Task<ScalarHeatmap> GetStayDurations(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate, Guid[] agvCorrelationIds);
}