using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Exceptions;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IGridAgvPositionRepository"/>>.
public class EfGridAgvPositionRepository(StatisticsDbContext context) : EfRepository<GridAgvPosition, StatisticsDbContext>(context), IGridAgvPositionRepository
{
    /// <inheritdoc cref="IGridAgvPositionRepository"/>>
    public async Task<CountHeatmap> GetPositionFrequency(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate)
    {
        IQueryable<GridAgvPosition> query = DbSet.Where(p => p.GridId == gridId)
            .Include(agp => agp.MapAgvPositions)
            .Where(p => p.MapAgvPositions!.Any(mp => mp.MapId== mapId));
        
        if (fromDate != null && fromDate != DateTime.MinValue)
        {
            query = query.Where(g => g.Timestamp >= fromDate);
        }

        if (toDate != null && toDate != DateTime.MaxValue)
        {
            query = query.Where(g => g.Timestamp <= toDate);
        }

        List<GridAgvPosition> positions = await query.ToListAsync();
        ICollection<ICollection<float>> frequencies = [];
        if (positions.Count > 0)
        {
            frequencies = await query
                .GroupBy(entry => new { entry.XIndex, entry.YIndex })
                .Select(group => new List<float>
                {
                    group.Key.XIndex, group.Key.YIndex, group.Count()
                } as ICollection<float>).ToListAsync();
        }
        
        return new CountHeatmap(gridId, mapId, frequencies);
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
    public async Task<GridAgvPosition> GetPositionAsync(Guid gridId, int xPosition,  int yPosition, float z, string agvSerialNumber)
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