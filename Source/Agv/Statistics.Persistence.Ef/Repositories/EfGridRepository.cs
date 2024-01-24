using Microsoft.EntityFrameworkCore;
using Statistics.Domain.Models;
using Statistics.Domain.Repositories;
using Statistics.Persistence.Ef.DataAccess;

namespace Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IGridRepository"/>>.
public class EfGridRepository(StatisticsDbContext context) : EfRepository<Grid, StatisticsDbContext>(context), IGridRepository
{
    /// <inheritdoc cref="IGridRepository"/>>.
    public async Task<ICollection<Guid>> GetAllIdsAsync()
    {
        return await DbSet.Select(g => g.GridId).ToListAsync();
    }
}