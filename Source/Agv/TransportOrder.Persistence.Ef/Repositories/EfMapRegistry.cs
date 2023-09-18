using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Map Registry using Entity Framework to access the AgvSaga persistence storage
/// </summary>
public class EfMapRegistry : IMapRegistry
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly ILogger<EfMapRegistry> _logger;
    private readonly IMapRegistrySemaphoreProvider _semaphoreProvider;

    /// <summary>
    /// Constructor of the EF MapRegistry
    /// </summary>
    /// <param name="dbContextFactory"></param>
    /// <param name="logger"></param>
    /// <param name="semaphoreProvider">The semaphore provider.</param>
    public EfMapRegistry(IDbContextFactory<TransportOrderDbContext> dbContextFactory, ILogger<EfMapRegistry> logger, IMapRegistrySemaphoreProvider semaphoreProvider)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _semaphoreProvider = semaphoreProvider;
    }

    private IQueryable<Map> Maps =>
        _dbContextFactory.CreateDbContext()
            .Set<Map>()
            .AsNoTracking(); // No need to track changes

    /// <inheritdoc/>
    public IQueryable<Map> GetAllMaps()
    {
        return Maps;
    }

    /// <inheritdoc/>
    public byte[]? GetMap(Guid correlationId)
    {
        return Maps.FirstOrDefault(q => q.CorrelationId == correlationId)?.MapValue;
    }

    /// <inheritdoc/>
    public bool Exists(Guid correlationId)
    {
        return Maps.FirstOrDefault(q => q.CorrelationId == correlationId) != null;
    }

    /// <inheritdoc/>
    public async Task<bool> AddOrUpdateAsync(Map map, CancellationToken cancellationToken)
    {
        return await _semaphoreProvider.ExecuteLockedAsync( async () =>
        {
            if (Exists(map.CorrelationId))
            {
                return await UpdateAsync(map);
            }
            return await AddAsync(map);
        }, cancellationToken);
    }

    private async Task<bool> AddAsync(Map map)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext
                .Add(map);

            int stateEntries = await dbContext.SaveChangesAsync();
            return stateEntries != 0;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not add map");
            return false;
        }
    }

    private async Task<bool> UpdateAsync(Map map)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.Update(map);
            int stateEntries = await dbContext.SaveChangesAsync();
            return stateEntries != 0;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not update map");
            return false;
        }
    }
}