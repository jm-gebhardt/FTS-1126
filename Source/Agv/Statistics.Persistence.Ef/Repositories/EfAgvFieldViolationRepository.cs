using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IAgvFieldViolationRepository"/>>.
public class EfAgvFieldViolationRepository(StatisticsDbContext context): EfRepository<AgvFieldViolation, StatisticsDbContext>(context), IAgvFieldViolationRepository
{
    /// <inheritdoc cref="IAgvFieldViolationRepository"/>>.
    public async Task<CountHeatmap> GetFieldViolationOccurence(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate, string[] agvSerialNumbers)
    {
        IQueryable<AgvFieldViolation> query = DbSet.Where(e => e.Position!.GridId == gridId)
            .Include(e => e.Position!.MapAgvPositions)
            .Where(e => e.Position!.MapAgvPositions!.Any(mp => mp.MapId == mapId));

        if (agvSerialNumbers.Length > 0)
        {
            query = query.Where(e => agvSerialNumbers.Contains(e.Position!.SerialNumber));
        }

        if (fromDate != null && fromDate != DateTime.MinValue)
        {
            query = query.Where(e => e.Timestamp >= fromDate);
        }

        if (toDate != null && toDate != DateTime.MaxValue)
        {
            query = query.Where(e => e.Timestamp <= toDate);
        }
        List<AgvFieldViolation> errors = await query.ToListAsync();
       
        ICollection<ICollection<float>> frequencies = [];
        if (errors.Count > 0)
        {
            frequencies = await query
                .GroupBy(entry => new { entry.Position!.XIndex, entry.Position!.YIndex })
                .Select(group => new List<float>
                {
                    group.Key.XIndex, group.Key.YIndex, group.Count()
                } as ICollection<float>).ToListAsync();
        }

        return new CountHeatmap(gridId, mapId, frequencies);
    }
}