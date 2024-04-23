using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IMapRepository"/>>.
public class EfMapRepository(StatisticsDbContext context) : EfRepository<Map, StatisticsDbContext>(context), IMapRepository
{
    /// <inheritdoc cref="IMapRepository"/>>
    public async Task<ICollection<Map>> GetMapsForGridAsync(Guid gridId)
    {
        return await DbSet.Where(m => m.GridId == gridId).ToListAsync();
    }
}