using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Map repository using Entity Framework to access the AgvSaga persistence storage.
/// </summary>
public class EfMapRepository : IMapRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly ILogger<EfMapRepository> _logger;
    private readonly ISemaphoreProvider<IMapRepositorySemaphore> _semaphoreProvider;

    /// <summary>
    /// Constructor of the EF MapRepository.
    /// </summary>
    /// <param name="dbContextFactory"></param>
    /// <param name="logger"></param>
    /// <param name="semaphoreProvider">The semaphore provider.</param>
    public EfMapRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory,
        ISemaphoreProvider<IMapRepositorySemaphore> semaphoreProvider,
        ILogger<EfMapRepository> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _semaphoreProvider = semaphoreProvider;
    }

    private IQueryable<Map> Maps =>
        _dbContextFactory.CreateDbContext()
            .Set<Map>()
            .AsNoTracking(); // No need to track changes

    private void UpdateRelatedEntities<TEntity, TId>(DbContext context,
        ICollection<TEntity> existingEntities,
        ICollection<TEntity> newEntities,
        Func<TEntity, TId> getId,
        bool attachBeforeAdd = false,
        bool updateExistingEntities = true)
        where TEntity : class
        where TId : IEquatable<TId>
    {
        // Remove entities that are no longer present
        foreach (var existingEntity in existingEntities.Where(existingEntity => newEntities.All(e => !Equals(getId(e), getId(existingEntity)))))
        {
            context.Remove(existingEntity);
        }

        // Add or update entities
        foreach (var newEntity in newEntities)
        {
            var existingEntity = existingEntities.FirstOrDefault(e => Equals(getId(e), getId(newEntity)));
            if (existingEntity == null)
            {
                if (attachBeforeAdd)
                {
                    context.Set<TEntity>().Attach(newEntity);
                }

                existingEntities.Add(newEntity);
            }
            else
            {
                if (updateExistingEntities)
                {
                    context.Entry(existingEntity).CurrentValues.SetValues(newEntity);
                }
            }
        }
    }

    /// <inheritdoc/>
    public async Task<ICollection<Map>> GetAllMapsAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<Map> maps = await context.Set<Map>()
            .Include(g => g.Nodes)
            .Include(g => g.Edges)
            .Include(g => g.MapAgvs)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();

        return maps;
    }

    /// <inheritdoc/>
    public byte[]? GetMapImage(Guid correlationId)
    {
        return Maps.FirstOrDefault(q => q.CorrelationId == correlationId)?.MapValue;
    }

    /// <inheritdoc/>
    public async Task<Map> GetMapAsync(Guid correlationId)
    {
        var map = await Maps.FirstOrDefaultAsync(q => q.CorrelationId == correlationId);
        if (map == null)
        {
            throw new MapNotFoundException();
        }
        return map;
    }

    /// <inheritdoc/>
    public bool Exists(Guid correlationId)
    {
        return Maps.FirstOrDefault(q => q.CorrelationId == correlationId) != null;
    }

    /// <inheritdoc/>
    public async Task<bool> AddOrUpdateAsync(Map map, CancellationToken cancellationToken)
    {
        return await _semaphoreProvider.ExecuteLockedAsync(async () => await AddOrUpdateAsync(map), cancellationToken);
    }

    /// <inheritdoc />
    public Task AssignAgvGraphToMapsAsync(Guid agvId)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync();

                var agvGraph = await context.Set<EfAgvGraph>()
                    .Include(ag => ag.Nodes)
                    .FirstOrDefaultAsync(ag => ag.CorrelationId == agvId);

                if (agvGraph == null)
                {
                    throw new AgvGraphNotDefinedException(agvId);
                }

                List<Map> maps = await context.Set<Map>()
                    .Include(m => m.Nodes)
                    .Include(m => m.MapAgvs)
                    .AsSplitQuery()
                    .Where(m => m.Nodes.Any(n => agvGraph.Nodes.Select(x => x.Id).Contains(n.Id)))
                    .ToListAsync();

                foreach (var map in maps.Where(map => map.MapAgvs.All(ma => ma.AgvId != agvId)))
                {
                    map.MapAgvs.Add(new MapAgv(map.CorrelationId, null, agvId, null));
                }

                await context.SaveChangesAsync();
            }, CancellationToken.None
        );
    }

    private async Task<bool> AddOrUpdateAsync(Map map)
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var existingMap = await context.Set<Map>()
                .Include(m => m.MapAgvs)
                .Include(m => m.Edges)
                .Include(m => m.Nodes)
                .AsSplitQuery()
                .SingleOrDefaultAsync(g => g.CorrelationId == map.CorrelationId);

            if (await context.Set<EfNode>()
                    .Where(node => map.Nodes.Select(n => n.Id).Contains(node.Id))
                    .CountAsync() != map.Nodes.Count ||
                await context.Set<EfEdge>()
                    .Where(edge => map.Edges.Select(e => e.Id).Contains(edge.Id))
                    .CountAsync() != map.Edges.Count)
            {
                _logger.LogWarning("Not all nodes or edges exist yet, so we cannot store the Map yet.");
                return false;
            }

            if (await context.Set<Domain.Models.Agv.Agv>()
                    .Where(agv => map.MapAgvs.Select(x => x.AgvId).Contains(agv.CorrelationId))
                    .CountAsync() != map.MapAgvs.Count)
            {
                _logger.LogWarning("Not all Agvs exist yet, so we cannot store the Map yet.");
                return false;
            }

            if (existingMap == null)
            {
                context.AttachRange(map.Edges);
                context.AttachRange(map.Nodes);
                context.Add(map);
            }
            else
            {
                // update properties
                context.Update(existingMap).CurrentValues.SetValues(map);

                UpdateRelatedEntities(context, existingMap.MapAgvs, map.MapAgvs, ma => ma.AgvId);
                UpdateRelatedEntities(context, existingMap.Nodes, map.Nodes, n => n.Id, true, false);
                UpdateRelatedEntities(context, existingMap.Edges, map.Edges, e => e.Id, true, false);
            }

            int stateEntries = await context.SaveChangesAsync();
            return stateEntries != 0;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not add or update map");
            return false;
        }
    }
}