using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <summary>
/// Base repository for managing heatmap-related entities with position data.
/// </summary>
/// <typeparam name="TEntity">Entity type implementing IPositionedEntity interface.</typeparam>
public abstract class HeatmapRepositoryBase<TEntity>(StatisticsDbContext context)
    : EfRepository<TEntity, StatisticsDbContext>(context)
    where TEntity : class, IPositionedEntity
{
    /// <summary>
    /// Builds a base query for retrieving entities based on specified filters.
    /// </summary>
    /// <param name="gridId">Identifier of the grid.</param>
    /// <param name="mapId">Identifier of the map.</param>
    /// <param name="fromDate">Start date for filtering results.</param>
    /// <param name="toDate">End date for filtering results.</param>
    /// <param name="serialNumbers">Optional list of serial numbers to filter results. If not provided, all available serial numbers are included.</param>
    /// <returns>IQueryable representing the filtered entity set.</returns>
    protected IQueryable<TEntity> BuildBaseQuery(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate, string[]? serialNumbers = null)
    {
        IQueryable<TEntity> query = DbSet
            .Where(e => e.Position!.GridId == gridId)
            .Include(e => e.Position!.MapAgvPositions)
            .Where(e => e.Position!.MapAgvPositions!.Any(mp => mp.MapId == mapId))
            .WhereIn(serialNumbers, e => e.Position!.SerialNumber)
            .WhereInTimeRange(fromDate, toDate, e => e.Timestamp);

        return query;
    }

    /// <summary>
    /// Builds a count heatmap based on the provided query and identifiers.
    /// </summary>
    /// <param name="query">Query representing the dataset to analyze.</param>
    /// <param name="gridId">Identifier of the grid.</param>
    /// <param name="mapId">Identifier of the map.</param>
    /// <returns>A <see cref="ScalarHeatmap"/> object containing the heatmap data.</returns>
    protected async Task<ScalarHeatmap> BuildScalarHeatmapAsync(
        IQueryable<TEntity> query,
        Guid gridId,
        Guid mapId)
    {
        List<ICollection<float>> frequencies = await query
            .GroupBy(e => new { e.Position!.XIndex, e.Position!.YIndex })
            .Select(g => new List<float> { g.Key.XIndex, g.Key.YIndex, g.Count() } as ICollection<float>)
            .ToListAsync();

        return new ScalarHeatmap(gridId, mapId, frequencies);
    }
}