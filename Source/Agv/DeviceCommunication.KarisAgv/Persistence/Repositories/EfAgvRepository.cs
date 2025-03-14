using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.Repositories;

/// <inheritdoc cref="IAgvRepository"/>>
public class EfAgvRepository : EfRepository<Agv, DeviceCommunicationDbContext>, IAgvRepository
{
    private readonly IAgvCacheProvider _agvCache;

    /// <inheritdoc cref="IAgvRepository"/>>
    public EfAgvRepository(DeviceCommunicationDbContext context, IAgvCacheProvider agvCache) : base(context)
    {
        _agvCache = agvCache;
    }

    /// <inheritdoc cref="IAgvRepository"/>>
    public async Task AddOrUpdate(Guid correlationId, string serialNumber)
    {
        if (_agvCache.ConcurrentDict.ContainsKey(correlationId))
        {
            return;
        }
        if (_agvCache.ConcurrentDict.TryAdd(correlationId, serialNumber))
        {
            var agv = new Agv(correlationId, serialNumber);
            Add(agv);
            await SaveAsync();
        }
    }

    /// <inheritdoc cref="IAgvRepository"/>>
    public async Task<Agv> GetAgv(Guid correlationId)
    {
        if (_agvCache.ConcurrentDict.TryGetValue(correlationId, out string? serialNumber))
        {
            return new Agv(correlationId, serialNumber);
        }

        return await GetByIdAsync(correlationId);
    }

    /// <inheritdoc cref="IAgvRepository"/>>
    public async Task LoadCache()
    {
        foreach (var agv in await Context.Set<Agv>().ToListAsync())
        {
            _agvCache.ConcurrentDict.TryAdd(agv.CorrelationId, agv.SerialNumber);
        }
    }
}
