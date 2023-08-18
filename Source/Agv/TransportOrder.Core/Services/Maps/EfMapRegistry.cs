using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IMap = Gebhardt.StoreWare.Agv.TransportOrder.Core.Models.IMap;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Maps;

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
    public IQueryable<IMap> GetAllMaps()
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
    public async Task<bool> AddOrUpdateAsync(Guid correlationId, string mapId, string base64PngImage, double resolution, double xOffset, double yOffset, DateTime modificationDate, CancellationToken cancellationToken)
    {
        try
        {
            await _semaphoreProvider.Semaphore.WaitAsync(cancellationToken);
            if (Exists(correlationId))
            {
                return Update(correlationId, mapId, base64PngImage, resolution, xOffset, yOffset, modificationDate);
            }
            else
            {
                return Add(correlationId, mapId, base64PngImage, resolution, xOffset, yOffset, modificationDate);
            }
        }
        finally
        {
            _semaphoreProvider.Semaphore.Release();
        }
    }

    private bool Add(Guid correlationId, string mapId, string base64PngImage, double resolution, double xOffset, double yOffset, DateTime modificationDate)
    {
        try
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            dbContext
                .Add(new Map(
                    correlationId,
                    mapId,
                    Convert.FromBase64String(base64PngImage),
                    resolution,
                    xOffset,
                    yOffset,
                    modificationDate
                ));

            int stateEntries = dbContext.SaveChanges();
            return stateEntries != 0;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not add map");
            return false;
        }
    }

    private bool Update(Guid correlationId, string mapId, string base64PngImage, double resolution, double xOffset, double yOffset, DateTime modificationDate)
    {
        try
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var map = dbContext
                .Set<Map>()
                .First(q => q.CorrelationId == correlationId);
            map.MapId = mapId;
            map.MapValue = Convert.FromBase64String(base64PngImage);
            map.Resolution = resolution;
            map.XOffset = xOffset;
            map.YOffset = yOffset;
            map.ModificationDate = modificationDate;

            int stateEntries = dbContext.SaveChanges();
            return stateEntries != 0;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not update map");
            return false;
        }
    }
}