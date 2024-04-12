using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Repositories;
using Gebhardt.StoreWare.Agv.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="EfMapAgvPositionsRepository"/>>.
public class EfMapAgvPositionsRepository(StatisticsDbContext context) : EfRepository<MapAgvPositions, StatisticsDbContext>(context), IMapAgvPositionsRepository
{
    /// <inheritdoc cref="EfMapAgvPositionsRepository"/>>.
    public async Task<ICollection<MapAgvPositions>> GetMapAgvPositionsForMapAsync(Guid mapId)
    {
        return await DbSet.Where(mp => mp.MapId == mapId).ToListAsync();
    }
}