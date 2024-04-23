using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="EfMapAgvPositionsRepository"/>>.
public class EfMapAgvPositionsRepository(StatisticsDbContext context) : EfRepository<MapAgvPositions, StatisticsDbContext>(context), IMapAgvPositionsRepository
{
    /// <inheritdoc cref="EfMapAgvPositionsRepository"/>>.
    public async Task<ICollection<MapAgvPositions>> GetMapAgvPositionsForMapAsync(Guid mapId)
    {
        return await DbSet.Where(mp => mp.MapId == mapId).ToListAsync();
    }
}