using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;

/// <inheritdoc cref="IAgvRepository"/>
public class EfAgvRepository(IDbContextFactory<DeviceCommunicationVda5050DbContext> dbContextFactory, IAgvCacheProvider agvCache) : IAgvRepository
{
    /// <inheritdoc />
    public async Task<Agv?> GetAgvAsync(Guid correlationId)
    {
        if (agvCache.ConcurrentDict.TryGetValue(correlationId, out var agv))
        {
            return agv;
        }
        await using var context = await dbContextFactory.CreateDbContextAsync();
        DbSet<Agv> dbSet = context.Set<Agv>();
        agv = await dbSet.SingleOrDefaultAsync(a => a.CorrelationId == correlationId);
        if (agv != null)
        {
            agvCache.ConcurrentDict.TryAdd(agv.CorrelationId, agv);
        }
        return agv;
    }

    /// <inheritdoc />
    public async Task<Agv> GetRequiredAgvAsync(Guid correlationId)
    {
        return await GetAgvAsync(correlationId) ?? throw new AgvNotFoundException(correlationId);
    }

    /// <inheritdoc cref="IAgvRepository"/>
    public async Task<Agv> GetAgvByCurrentOrderAsync(Guid orderId)
    {
        var agv = await agvCache.GetAgvByCurrentOrderAsync(orderId);
        if (agv != null)
        {
            return agv;
        }
        await using var context = await dbContextFactory.CreateDbContextAsync();
        DbSet<Agv> dbSet = context.Set<Agv>();
        agv = await dbSet.SingleOrDefaultAsync(a => a.CurrentOrder == orderId);
        return agv ?? throw new AgvNotFoundException(orderId);
    }

    /// <inheritdoc cref="IAgvRepository"/>
    public async Task<IReadOnlyCollection<Agv>> GetAllAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        DbSet<Agv> dbSet = context.Set<Agv>();
        List<Agv> agvs = await dbSet.ToListAsync();
        foreach (var agv in agvs)
        {
            agvCache.ConcurrentDict.TryAdd(agv.CorrelationId, agv);
        }
        return agvs;
    }

    /// <inheritdoc cref="IAgvRepository"/>
    public async Task AddOrUpdateAsync(Agv agv)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        DbSet<Agv> dbSet = context.Set<Agv>();
        bool existsInCache = agvCache.ConcurrentDict.ContainsKey(agv.CorrelationId);
        bool existsInDatabase = existsInCache || await dbSet.AnyAsync(a => a.CorrelationId == agv.CorrelationId);

        if (existsInDatabase)
        {
            dbSet.Update(agv);
        }
        else
        {
            dbSet.Add(agv);
        }

        await context.SaveChangesAsync();
        agvCache.ConcurrentDict.AddOrUpdate(agv.CorrelationId, agv, (_, _) => agv);
    }
}