using System.Linq.Expressions;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <summary>
/// Base repository for managing heatmap-related entities with position data.
/// </summary>
/// <typeparam name="TEntity">Entity type implementing IPositionedEntity interface.</typeparam>
public abstract class HeatmapRepositoryBase<TEntity>(StatisticsDbContext context)
    : EfRepository<TEntity, StatisticsDbContext>(context), IHeatmapRepository<TEntity>
    where TEntity : class, IGridCellAssigned
{
    /// <summary>
    /// Builds a base query for retrieving entities based on specified filters.
    /// </summary>
    /// <param name="gridId">Identifier of the grid.</param>
    /// <param name="mapId">Identifier of the map.</param>
    /// <param name="fromDate">Start date for filtering results.</param>
    /// <param name="toDate">End date for filtering results.</param>
    /// <param name="agvCorrelationIds">The AGV correlation IDs to filter by. If null, no filtering is applied.</param>
    /// <returns>IQueryable representing the filtered entity set.</returns>
    protected IQueryable<TEntity> BuildBaseQuery(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate, Guid[]? agvCorrelationIds = null)
    {
        var query = DbSet
            .Where(e => e.GridCell.GridId == gridId)
            .Include(e => e.GridCell.MapGridCell)
            .Where(e => e.GridCell.MapGridCell!.Any(mp => mp.MapId == mapId))
            .WhereIn(agvCorrelationIds, e => e.AgvCorrelationId)
            .WhereInTimeRange(fromDate, toDate, e => e.Timestamp);

        return query;
    }

    /// <summary>
    /// Builds a count heatmap based on the provided query and identifiers.
    /// </summary>
    /// <param name="query">Query representing the dataset to analyze.</param>
    /// <param name="gridId">Identifier of the grid.</param>
    /// <param name="mapId">Identifier of the map.</param>
    /// <param name="aggregator">Optional aggregator expression.</param>
    /// <returns>A <see cref="ScalarHeatmap"/> object containing the heatmap data.</returns>
    protected async Task<ScalarHeatmap> BuildScalarHeatmapAsync(
        IQueryable<TEntity> query,
        Guid gridId,
        Guid mapId,
        Expression<Func<IGrouping<GridCell, TEntity>, float>>? aggregator = null)
    {
        aggregator ??= g => g.Count();

        var param = aggregator.Parameters.First();

        var basePropAccess = Expression.PropertyOrField(param, "Key");

        var xAccess = Expression.Convert(Expression.PropertyOrField(basePropAccess, "XIndex"), typeof(float));
        var yAccess = Expression.Convert(Expression.PropertyOrField(basePropAccess, "YIndex"), typeof(float));

        var addMethod = typeof(List<float>).GetMethod(nameof(List<>.Add))!;

        var listInit = Expression.ListInit(
            Expression.New(typeof(List<float>)),
            Expression.ElementInit(addMethod, xAccess),
            Expression.ElementInit(addMethod, yAccess),
            Expression.ElementInit(addMethod, aggregator.Body)
        );

        var selector = Expression.Lambda<Func<IGrouping<GridCell, TEntity>, List<float>>>(listInit, false, param);

        var frequencies = await query
            .GroupBy(e => e.GridCell)
            .Select(selector)
            .Select(l => l as ICollection<float>)
            .ToListAsync();

        return new ScalarHeatmap(gridId, mapId, frequencies);
    }

    /// <inheritdoc />
    public async Task DeleteAgvPositionsOlderThan(DateTime timestamp)
    {
        if (Context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var oldRows = await DbSet.Where(p => p.Timestamp < timestamp).ToListAsync();
            Context.RemoveRange(oldRows);
            await Context.SaveChangesAsync();
        }
        else
        {
            await DbSet.Where(p => p.Timestamp < timestamp).ExecuteDeleteAsync();
        }
    }
}