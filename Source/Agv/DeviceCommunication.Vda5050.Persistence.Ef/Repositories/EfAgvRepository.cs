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
        return await dbSet.SingleOrDefaultAsync(a => a.CorrelationId == correlationId);
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
    private async Task<IReadOnlyCollection<Agv>> GetAllAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        DbSet<Agv> dbSet = context.Set<Agv>();
        return await dbSet.ToListAsync();
    }

    /// <inheritdoc cref="IAgvRepository"/>
    public async Task AddOrUpdateAsync(Agv agv, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        DbSet<Agv> dbSet = context.Set<Agv>();
        if (agvCache.ConcurrentDict.ContainsKey(agv.CorrelationId))
        {
            agvCache.ConcurrentDict.AddOrUpdate(agv.CorrelationId, agv, (_, _) => agv);
            dbSet.Update(agv);
            await context.SaveChangesAsync(cancellationToken);
            return;
        }
        if (agvCache.ConcurrentDict.TryAdd(agv.CorrelationId, agv))
        {
            dbSet.Add(agv);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc cref="IAgvRepository"/>
    public async Task InitializeCacheAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        List<Agv> agvs = await context.Set<Agv>().ToListAsync();
        foreach (var agv in agvs)
        {
            agvCache.ConcurrentDict.AddOrUpdate(agv.CorrelationId, agv, (_, _) => agv);
        }
    }

    /// <inheritdoc cref="IAgvRepository"/>
    public async Task<ICollection<Agv>> GetAllForVehicleTypesAsync(ICollection<string> vehicleTypeIds)
    {
        HashSet<string> vehicles = vehicleTypeIds.ToHashSet();
        return (await GetAllAsync()).Where(a => vehicles.Contains(a.VehicleTypeId)).ToList();
    }
}