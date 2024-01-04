using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

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
        return AgvData.AnyAsync(q => q.CorrelationId == correlationId);
    }

    /// <inheritdoc/>
    public Task<AgvData?> GetAgvDataAsync(Guid correlationId)
    {
        return AgvData.FirstOrDefaultAsync(q => q.CorrelationId == correlationId);
    }

    /// <inheritdoc/>
    public async Task<bool> AddOrUpdateAsync(AgvData agvData)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        DbSet<AgvData> dbSet = dbContext.Set<AgvData>();

        if (dbSet.Any(a => a.CorrelationId == agvData.CorrelationId))
        {
            dbSet.Update(agvData);
        }
        else
        {
            dbSet.Add(agvData);
        }

        return await dbContext.SaveChangesAsync() > 0;
    }

    /// <inheritdoc/>
    public async Task<ICollection<AgvData>> GetSelectedAgvDataAsync(ICollection<Guid> correlationIds)
    {
        return await AgvData.Where(a => correlationIds.Contains(a.CorrelationId)).ToListAsync();
    }
}