using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.ReadModels;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using AgvError = Gebhardt.Acc.Statistics.Domain.Models.AgvError;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IAgvErrorRepository"/>>.
public class EfAgvErrorRepository(StatisticsDbContext context) : EfRepository<AgvError, StatisticsDbContext>(context), IAgvErrorRepository
{
    /// <inheritdoc cref="IAgvErrorRepository"/>>.
    public async Task<CountHeatmap> GetErrorOccurence(Guid gridId, Guid mapId, DateTime? fromDate, DateTime? toDate, string[] agvSerialNumbers, ErrorLevel errorLevel, string? errorCode)
    {
        IQueryable<AgvError> query = DbSet.Where(e => e.Position!.GridId == gridId)
            .Include(e => e.Position!.MapAgvPositions)
            .Where(e => e.Position!.MapAgvPositions!.Any(mp => mp.MapId == mapId))
            .Where(e => e.ErrorLevel == errorLevel);

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

        if (errorCode != null)
        {
            query = query.Where(e => e.ErrorCode == errorCode);
        }

        List<AgvError> errors = await query.ToListAsync();
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