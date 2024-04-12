using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Gebhardt.StoreWare.Agv.Statistics.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Repositories;
using Gebhardt.StoreWare.Agv.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IGridAgvPositionRepository"/>>.
public class EfGridAgvPositionRepository(StatisticsDbContext context) : EfRepository<GridAgvPosition, StatisticsDbContext>(context), IGridAgvPositionRepository
{
    /// <inheritdoc cref="IGridAgvPositionRepository"/>>
    public async Task<PositionFrequency> GetPositionFrequency(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate)
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

        ICollection<ICollection<float>> frequencies = await query
            .GroupBy(entry => new { entry.XIndex, entry.YIndex })
            .Select(group => new List<float>
            {
                group.Key.XIndex, group.Key.YIndex, group.Count()
            } as ICollection<float>).ToListAsync();
        
        return new PositionFrequency(gridId, mapId, frequencies);
    }
}