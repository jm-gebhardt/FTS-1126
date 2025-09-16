using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

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

    /// <inheritdoc />
    public async Task SetCustomBackgroundAsync(Guid mapCorrelationId, MapBackground mapBackground)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var map = await context.Set<Map>()
            .SingleOrDefaultAsync(m => m.CorrelationId == mapCorrelationId);

        if (map == null)
        {
            throw new MapNotFoundException(mapBackground.CorrelationId);
        }

        map.CustomBackground = mapBackground;

        context.Update(map);

        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCustomBackgroundAsync(Guid correlationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var map = await context.Set<Map>()
            .SingleOrDefaultAsync(m => m.CorrelationId == correlationId);

        if (map == null)
        {
            throw new MapNotFoundException(correlationId);
        }

        map.CustomBackground = null;
        context.Update(map);
        return await context.SaveChangesAsync() > 0;
    }

    /// <inheritdoc/>
    public async Task<Map> GetMapAsync(Guid correlationId)
    {
        var map = await Maps.Include(m => m.MapAgvs)
            .SingleOrDefaultAsync(q => q.CorrelationId == correlationId);
        if (map == null)
        {
            throw new MapNotFoundException(correlationId);
        }
        return map;
    }

    /// <inheritdoc />
    public async Task<byte[]> GetBackgroundImageAsync(Guid correlationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var map = await context.Set<Map>()
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.CorrelationId == correlationId);

        var mapBackground = map?.CustomBackground ?? map?.BaseBackground;
        if (mapBackground == null)
        {
            throw new MapNotFoundException(correlationId);
        }

        return mapBackground.Image;
    }

    /// <inheritdoc/>
    public bool Exists(Guid correlationId)
    {
        return Maps.SingleOrDefault(q => q.CorrelationId == correlationId) != null;
    }

    /// <inheritdoc/>
    public async Task<bool> AddOrUpdateAsync(Map map, CancellationToken cancellationToken)
    {
        return await _semaphoreProvider.ExecuteLockedAsync(async () => await AddOrUpdateAsync(map), cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateMapBackgroundParametersAsync(Guid correlationId, double resolution, double xOffset, double yOffset)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var map = await context.Set<Map>()
            .SingleOrDefaultAsync(m => m.CorrelationId == correlationId);

        if (map == null)
        {
            throw new MapNotFoundException(correlationId);
        }

        var mapBackground = map.BaseBackground ?? map.CustomBackground;

        if (mapBackground == null)
        {
            throw new MapNotFoundException(correlationId);
        }

        mapBackground.Resolution = resolution;
        mapBackground.XOffset = xOffset;
        mapBackground.YOffset = yOffset;

        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public Task AssignAgvGraphToMapsAsync(Guid agvId)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync();

                var agvGraph = await context.Set<EfAgvGraph>()
                    .Include(ag => ag.Nodes)
                    .SingleOrDefaultAsync(ag => ag.CorrelationId == agvId);

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

    /// <inheritdoc />
    public async Task<Guid> GetMapIdForNodeAsync(Guid nodeId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var map = await context.Set<Map>()
            .Include(m => m.Nodes)
            .FirstOrDefaultAsync(m => m.Nodes.Any(n => n.Id == nodeId));
        if (map == null)
        {
            throw new MapNotFoundException(nodeId);
        }
        return map.CorrelationId;
    }

    private async Task<bool> AddOrUpdateAsync(Map map)
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            if (!await CheckGraphSaved(map, context))
            {
                _logger.LogWarning("Not all nodes or edges exist yet, so we cannot store the Map yet");
                return false;
            }

            if (!await CheckAgvsSaved(map, context))
            {
                _logger.LogWarning("Not all Agvs exist yet, so we cannot store the Map yet");
                return false;
            }

            var existingMap = await context.Set<Map>()
                .Include(m => m.MapAgvs)
                .Include(m => m.Edges)
                .Include(m => m.Nodes)
                .AsSplitQuery()
                .SingleOrDefaultAsync(g => g.CorrelationId == map.CorrelationId);

            if (existingMap == null)
            {
                context.AttachRange(map.Edges);
                context.AttachRange(map.Nodes);
                context.Add(map);
            }
            else
            {
                UpdateMap(existingMap, map, context);
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

    private static async Task<bool> CheckAgvsSaved(Map map, TransportOrderDbContext context)
    {
        return await context.Set<Domain.Models.Agv.Agv>()
            .Where(agv => map.MapAgvs.Select(x => x.AgvId).Contains(agv.CorrelationId))
            .CountAsync() == map.MapAgvs.Count;
    }

    private static async Task<bool> CheckGraphSaved(Map map, TransportOrderDbContext context)
    {
        bool nodesSaved = await context
            .Set<EfNode>()
            .Where(node => map.Nodes.Select(n => n.Id).Contains(node.Id))
            .CountAsync() == map.Nodes.Count;

        bool edgesSaved = await context
            .Set<EfEdge>()
            .Where(edge => map.Edges.Select(e => e.Id).Contains(edge.Id))
            .CountAsync() == map.Edges.Count;

        return edgesSaved && nodesSaved;
    }

    private static void UpdateMap(Map oldMap, Map newMap, TransportOrderDbContext context)
    {
        if (newMap.BaseBackground != null && oldMap.BaseBackgroundId != newMap.BaseBackgroundId)
        {
            oldMap.BaseBackground = newMap.BaseBackground;
        }
        if (newMap.CustomBackground != null && oldMap.CustomBackgroundId != newMap.CustomBackgroundId)
        {
            oldMap.CustomBackground = newMap.CustomBackground;
        }

        UpdateRelations(oldMap, newMap, context);

        context.Update(oldMap);
    }

    private static void UpdateRelations(Map existingMap, Map map, TransportOrderDbContext context)
    {
        (IEnumerable<EfNode> nodesToRemove, IEnumerable<EfNode> nodesToAdd) = SymmetricDifference(existingMap.Nodes, map.Nodes, n => n.Id);
        (IEnumerable<EfEdge> edgesToRemove, IEnumerable<EfEdge> edgesToAdd) = SymmetricDifference(existingMap.Edges, map.Edges, n => n.Id);
        (IEnumerable<MapAgv> agvsToRemove, IEnumerable<MapAgv> agvsToAdd) = SymmetricDifference(existingMap.MapAgvs, map.MapAgvs, ma => ma.AgvId);

        foreach (var node in nodesToAdd)
        {
            context.Attach(node);
            node.Maps.Add(existingMap);
            existingMap.Nodes.Add(node);
        }

        foreach (var node in nodesToRemove)
        {
            node.Maps.Remove(existingMap);
            existingMap.Nodes.Remove(node);
        }

        foreach (var edge in edgesToAdd)
        {
            context.Attach(edge);
            edge.Maps.Add(existingMap);
            existingMap.Edges.Add(edge);
        }

        foreach (var edge in edgesToRemove)
        {
            edge.Maps.Remove(existingMap);
            existingMap.Edges.Remove(edge);
        }

        foreach (var agv in agvsToRemove)
        {
            existingMap.MapAgvs.Remove(agv);
        }

        foreach (var agv in agvsToAdd)
        {
            existingMap.MapAgvs.Add(agv);
        }
    }

    /// <summary>
    /// Removes all the entries in two collections that appear in the respective other one
    /// </summary>
    /// <param name="leftCollection">The left collection.</param>
    /// <param name="rightCollection">The right collection,</param>
    /// <param name="idProvider">Function to provide a comparable id value for the objects in the collections.</param>
    /// <returns>(LeftOnly, RightOnly)</returns>
    private static (IEnumerable<T>, IEnumerable<T>) SymmetricDifference<T, TId>(ICollection<T> leftCollection, ICollection<T> rightCollection, Func<T, TId> idProvider) where TId : IEquatable<TId>
    {
        List<TId> leftIds = leftCollection.Select(idProvider).ToList();
        List<TId> rightIds = rightCollection.Select(idProvider).ToList();

        IEnumerable<TId> leftOnly = leftIds.Except(rightIds);
        IEnumerable<TId> rightOnly = rightIds.Except(leftIds);

        return (
            leftOnly.Select(id => leftCollection.First(entry => Equals(id, idProvider(entry)))),
            rightOnly.Select(id => rightCollection.First(entry => Equals(id, idProvider(entry))))
        );
    }
}