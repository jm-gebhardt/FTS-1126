using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Map Registry using Entity Framework to access the AgvSaga persistence storage
/// </summary>
public class EfAgvDataRegistry : IAgvDataRegistry
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;

    private IQueryable<AgvData> AgvData => _dbContextFactory.CreateDbContext().Set<AgvData>().AsNoTracking();

    /// <summary>
    /// Constructor of the EF AgvDataRegistry
    /// </summary>
    /// <param name="dbContextFactory"></param>
    public EfAgvDataRegistry(IDbContextFactory<TransportOrderDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc/>
    public bool Exists(Guid correlationId)
    {
        return AgvData.Any(q => q.CorrelationId == correlationId);
    }

    /// <inheritdoc/>
    public AgvData? GetAgvData(Guid correlationId)
    {
        return AgvData.FirstOrDefault(q => q.CorrelationId == correlationId);
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

        return await  dbContext.SaveChangesAsync() > 0;
    }
}