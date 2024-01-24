using Microsoft.EntityFrameworkCore;
using Statistics.Domain.Models;
using Statistics.Domain.Repositories;
using Statistics.Persistence.Ef.DataAccess;

namespace Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IMapRepository"/>>.
public class EfMapRepository(StatisticsDbContext context) : EfRepository<Map, StatisticsDbContext>(context), IMapRepository
{
    /// <inheritdoc cref="IMapRepository"/>>
    public async Task<ICollection<Map>> GetMapsForGridAsync(Guid gridId)
    {
        return await DbSet.Where(m => m.GridId == gridId).ToListAsync();
    }
}