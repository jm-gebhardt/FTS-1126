using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Repositories;
using Gebhardt.StoreWare.Agv.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IMapRepository"/>>.
public class EfMapRepository(StatisticsDbContext context) : EfRepository<Map, StatisticsDbContext>(context), IMapRepository
{
    /// <inheritdoc cref="IMapRepository"/>>
    public async Task<ICollection<Map>> GetMapsForGridAsync(Guid gridId)
    {
        return await DbSet.Where(m => m.GridId == gridId).ToListAsync();
    }
}