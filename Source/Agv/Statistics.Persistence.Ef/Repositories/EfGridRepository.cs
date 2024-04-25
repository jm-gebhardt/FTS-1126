using Gebhardt.Acc.Statistics.Domain.Exceptions;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IGridRepository"/>>.
public class EfGridRepository(StatisticsDbContext context) : EfRepository<Grid, StatisticsDbContext>(context), IGridRepository
{
    /// <inheritdoc cref="IGridRepository"/>>.
    public async Task<ICollection<Guid>> GetAllIdsAsync()
    {
        return await DbSet.Select(g => g.GridId).ToListAsync();
    }

    /// <inheritdoc cref="IGridRepository"/>>
    public async Task ThrowIfNotExistAsync(Guid gridId)
    {
        try
        {
            await DbSet.SingleAsync(m => m.GridId == gridId);
        }
        catch (InvalidOperationException)
        {
            throw new GridNotFoundException(gridId);
        }
    }
}