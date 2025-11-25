using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IGridCellStayRepository"/>
public class EfGridCellStayRepository(StatisticsDbContext context) : HeatmapRepositoryBase<GridCellStay>(context), IGridCellStayRepository
{
    /// <inheritdoc cref="IGridCellStayRepository"/>
    public async Task<ScalarHeatmap> GetStayDurations(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate, Guid[] agvCorrelationIds)
    {
        IQueryable<GridCellStay> query = BuildBaseQuery(gridId, mapId, fromDate, toDate, agvCorrelationIds);

        var heatmap = await BuildScalarHeatmapAsync(query, gridId, mapId,
            g => (float) g.Sum(a => a.DurationSeconds));

        return heatmap;
    }
}