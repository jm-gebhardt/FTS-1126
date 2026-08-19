using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Map repository using Entity Framework to access the AgvSaga persistence storage.
/// </summary>
public class EfAgvDataRepository : IAgvDataRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;

    private IQueryable<AgvData> AgvData => _dbContextFactory.CreateDbContext().Set<AgvData>().AsNoTracking();

    /// <summary>
    /// Initializes a new <see cref="EfAgvDataRepository"/> instance.
    /// </summary>
    public EfAgvDataRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(Guid correlationId)
    {
        return AgvData.AnyAsync(q => q.AgvCorrelationId == correlationId);
    }

    /// <inheritdoc/>
    public Task<AgvData?> GetAgvDataAsync(Guid correlationId)
    {
        return AgvData.SingleOrDefaultAsync(q => q.AgvCorrelationId == correlationId);
    }

    /// <inheritdoc/>
    public async Task AddOrUpdateAsync(AgvData agvData)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        DbSet<AgvData> dbSet = dbContext.Set<AgvData>();

        if (dbSet.Any(a => a.AgvCorrelationId == agvData.AgvCorrelationId))
        {
            dbSet.Update(agvData);
        }
        else
        {
            dbSet.Add(agvData);
        }

        await dbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<ICollection<AgvData>> GetSelectedAgvDataAsync(ICollection<Guid> correlationIds)
    {
        return await AgvData.Where(a => correlationIds.Contains(a.AgvCorrelationId)).ToListAsync();
    }
}