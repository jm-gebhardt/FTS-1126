using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Exceptions;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IGridAgvPositionRepository"/>
public class EfGridAgvPositionRepository(StatisticsDbContext context) : EfRepository<GridAgvPosition, StatisticsDbContext>(context), IGridAgvPositionRepository
{
    /// <inheritdoc cref="IGridAgvPositionRepository"/>
    public async Task<ScalarHeatmap> GetPositionFrequency(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate)
    {
        IQueryable<GridAgvPosition> query = BuildBaseQuery(gridId, mapId, fromDate, toDate);

        var heatmap = await BuildScalarHeatmapAsync(query, gridId, mapId);

        return heatmap;
    }

    private IQueryable<GridAgvPosition> BuildBaseQuery(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate, string[]? serialNumbers = null)
    {
        IQueryable<GridAgvPosition> query = DbSet
            .Where(e => e.GridId == gridId)
            .Include(e => e.MapAgvPositions!.Where(mp => mp.MapId == mapId))
            .Where(e => e.MapAgvPositions!.Any(mp => mp.MapId == mapId))
            .WhereIn(serialNumbers, e => e.SerialNumber)
            .WhereInTimeRange(fromDate, toDate, e => e.Timestamp);

        return query;
    }

    private static async Task<ScalarHeatmap> BuildScalarHeatmapAsync(IQueryable<GridAgvPosition> query, Guid gridId, Guid mapId)
    {
        List<ICollection<float>> frequencies = await query
            .GroupBy(e => new { e.XIndex, e.YIndex })
            .Select(g => new List<float> { g.Key.XIndex, g.Key.YIndex, g.Sum(gp => gp.Weight) } as ICollection<float>)
            .ToListAsync();

        return new ScalarHeatmap(gridId, mapId, frequencies);
    }

    /// <inheritdoc cref="IGridAgvPositionRepository"/>
    public async Task DeleteAgvPositionsOlderThan(DateTime timestamp)
    {
        List<Guid> positionsToDelete = await DbSet
            .Where(p => p.Timestamp < timestamp)
            .Select(p => p.PositionId)
            .ToListAsync();

        List<Guid> delete = positionsToDelete;
        List<Guid> positionsWithErrors = await Context.Set<AgvError>()
            .Where(e => delete.Contains(e.PositionId))
            .Select(e => e.PositionId)
            .ToListAsync();

        List<Guid> positionsWithFieldViolations = await Context.Set<AgvFieldViolation>()
            .Where(e => delete.Contains(e.PositionId))
            .Select(e => e.PositionId)
            .ToListAsync();

        positionsToDelete = positionsToDelete.Except(positionsWithErrors).Except(positionsWithFieldViolations).ToList();
        IQueryable<GridAgvPosition> entitiesToDelete = DbSet.Where(p => positionsToDelete.Contains(p.PositionId));
        Context.RemoveRange(entitiesToDelete);
        await Context.SaveChangesAsync();
    }

    /// <inheritdoc cref="IGridAgvPositionRepository"/>>
    public async Task<GridAgvPosition> GetPositionAsync(Guid gridId, int xPosition, int yPosition, float z, string agvSerialNumber)
    {
        try
        {
            return await DbSet.Where(p => p.GridId == gridId)
                .Where(p => p.XIndex == xPosition && p.YIndex == yPosition && Math.Abs(p.Z - z) <= Domain.Constants.Grid.ZIdentityPrecision && p.SerialNumber == agvSerialNumber)
                .OrderByDescending(p => p.Timestamp)
                .FirstAsync();
        }
        catch (InvalidOperationException)
        {
            throw new GridAgvPositionNotFoundException($"Position at [{xPosition}, {yPosition}] z: {z} for agv {agvSerialNumber} on grid {gridId} ");
        }
    }
}