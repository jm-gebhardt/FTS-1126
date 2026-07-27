using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
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
    public async Task<ICollection<Map>> GetAllMapsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        List<Map> maps = await context.Set<Map>()
            .Include(g => g.Nodes)
            .Include(g => g.Edges)
            .Include(g => g.MapAgvs)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return maps;
    }

    /// <inheritdoc/>
    public async Task<(string? MapName, float MinY, float MaxY, float MinX, float MaxX)> GetDefaultMap()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var map = await context.Set<Map>()
            .Include(g => g.Nodes)
            .Include(g => g.Edges)
            .Include(g => g.MapAgvs)
            .AsNoTracking()
            .FirstOrDefaultAsync();
        if(map == null)
        {
            return (null, 0, 0, 0, 0);
        }
        return (map.MapName, map.Nodes.Min(n => n.Y), map.Nodes.Max(n => n.Y), map.Nodes.Min(n => n.X), map.Nodes.Max(n => n.X));
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
    public async Task<Map> GetMapAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        var map = await Maps.Include(m => m.MapAgvs)
            .SingleOrDefaultAsync(q => q.CorrelationId == correlationId, cancellationToken);
        if (map == null)
        {
            throw new MapNotFoundException(correlationId);
        }
        return map;
    }

    /// <inheritdoc />
    public async Task<byte[]> GetBackgroundImageAsync(Guid correlationId, bool getBaseImage = false, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var map = await context.Set<Map>()
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.CorrelationId == correlationId, cancellationToken);
        
        if (getBaseImage)
        {
            return map?.BaseBackground?.Image ?? throw new MapNotFoundException(correlationId);
        }

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
    public async Task<bool> AddOrUpdateAsync(Map map, Guid agvCorrelationId, CancellationToken cancellationToken)
    {
        return await _semaphoreProvider.ExecuteLockedAsync(async () => await AddOrUpdateAsync(map, agvCorrelationId), cancellationToken);
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

                var agvGraph = await context.Set<AgvGraph>()
                    .Include(ag => ag.Nodes)
                    .SingleOrDefaultAsync(ag => ag.AgvCorrelationId == agvId);

                if (agvGraph == null)
                {
                    throw new AgvGraphNotDefinedException(agvId);
                }

                List<Map> maps = await context.Set<Map>()
                    .Include(m => m.Nodes)
                    .Include(m => m.MapAgvs)
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

    private async Task<bool> AddOrUpdateAsync(Map map, Guid agvCorrelationId)
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            if (!await CheckAgvsSaved(map, context))
            {
                _logger.LogWarning("Not all Agvs exist yet, so we cannot store the Map yet");
                return false;
            }

            // AgvGraph is the ground truth for node/edge existence.
            // If no AgvGraph has been defined yet the system is in startup, and we cannot
            // determine which nodes are valid - fall back to the retry path so that
            // GraphMissing triggers the graph resend from the external system.
            bool anyAgvGraphDefined = await context.Set<AgvGraph>().AnyAsync();
            if (!anyAgvGraphDefined)
            {
                if (!await CheckGraphSaved(map, context))
                {
                    _logger.LogWarning("No AgvGraph defined yet; not all nodes/edges exist, cannot store Map yet");
                    return false;
                }
            }

            // Get all nodes and edges which are used by other AGVs.
            // Those graph elements must not be removed from an existing map update.
            HashSet<Guid> otherAgvNodeIds = (await context.Set<AgvGraph>()
                .Where(agvGraph => agvGraph.AgvCorrelationId != agvCorrelationId)
                .SelectMany(agvGraph => agvGraph.Nodes.Select(node => node.Id))
                .Distinct()
                .ToListAsync())
                .ToHashSet();

            HashSet<Guid> otherAgvEdgeIds = (await context.Set<AgvGraph>()
                .Where(agvGraph => agvGraph.AgvCorrelationId != agvCorrelationId)
                .SelectMany(agvGraph => agvGraph.Edges.Select(edge => edge.Id))
                .Distinct()
                .ToListAsync())
                .ToHashSet();

            HashSet<Guid> protectedAgvIds = (await context.Set<AgvGraph>()
                    .Where(agvGraph => agvGraph.AgvCorrelationId != agvCorrelationId)
                    .Select(agvGraph => agvGraph.AgvCorrelationId)
                    .Distinct()
                    .ToListAsync())
                .ToHashSet();

            // Once an AgvGraph is established, nodes not referenced by any AgvGraph may have
            // been consolidated away. Filter the map's collections to only those that still
            // exists in the global graph so that a saving the map does not re-introduce nodes
            // that were previously deliberately removed.
            List<Guid> nodeIds = map.Nodes.Select(n => n.Id).ToList();
            List<Guid> edgeIds = map.Edges.Select(e => e.Id).ToList();

            HashSet<Guid> existingNodeIds = (await context.Set<GraphNode>()
                .Where(n => nodeIds.Contains(n.Id))
                .Select(n => n.Id)
                .ToListAsync())
                .ToHashSet();

            HashSet<Guid> existingEdgeIds = (await context.Set<GraphEdge>()
                .Where(e => edgeIds.Contains(e.Id))
                .Select(e => e.Id)
                .ToListAsync())
                .ToHashSet();

            if (existingNodeIds.Count < map.Nodes.Count || existingEdgeIds.Count < map.Edges.Count)
            {
                _logger.LogInformation(
                    "Map {MapName}: {RemovedNodes} node(s) and {RemovedEdges} edge(s) are no longer referenced by any AgvGraph and will be excluded",
                    map.MapName,
                    map.Nodes.Count - existingNodeIds.Count,
                    map.Edges.Count - existingEdgeIds.Count);
            }

            var existingMap = await context.Set<Map>()
                .Include(m => m.MapAgvs)
                .Include(m => m.Edges)
                .Include(m => m.Nodes)
                .SingleOrDefaultAsync(g => g.CorrelationId == map.CorrelationId);

            ICollection<GraphNode> protectedNodes = existingMap == null
                ? []
                : existingMap.Nodes.Where(node => otherAgvNodeIds.Contains(node.Id)).ToList();

            ICollection<GraphEdge> protectedEdges = existingMap == null
                ? []
                : existingMap.Edges.Where(edge => otherAgvEdgeIds.Contains(edge.Id)).ToList();

            var effectiveMap = map with
            {
                Nodes = map.Nodes.Where(node => existingNodeIds.Contains(node.Id)).UnionBy(protectedNodes, node => node.Id).ToList(),
                Edges = map.Edges.Where(edge => existingEdgeIds.Contains(edge.Id)).UnionBy(protectedEdges, edge => edge.Id).ToList(),
                MapAgvs = map.MapAgvs
                    .UnionBy((existingMap?.MapAgvs ?? []).Where(ma => protectedAgvIds.Contains(ma.AgvId)), ma => ma.AgvId)
                    .ToList()
            };

            if (existingMap == null)
            {
                context.AttachRange(effectiveMap.Edges);
                context.AttachRange(effectiveMap.Nodes);
                context.Add(effectiveMap);
            }
            else
            {
                UpdateMap(existingMap, effectiveMap, context);
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
            .Set<GraphNode>()
            .Where(node => map.Nodes.Select(n => n.Id).Contains(node.Id))
            .CountAsync() == map.Nodes.Count;

        bool edgesSaved = await context
            .Set<GraphEdge>()
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
        (IEnumerable<GraphNode> nodesToRemove, IEnumerable<GraphNode> nodesToAdd) = SymmetricDifference(existingMap.Nodes, map.Nodes, n => n.Id);
        (IEnumerable<GraphEdge> edgesToRemove, IEnumerable<GraphEdge> edgesToAdd) = SymmetricDifference(existingMap.Edges, map.Edges, n => n.Id);
        (IEnumerable<MapAgv> agvsToRemove, IEnumerable<MapAgv> agvsToAdd) = SymmetricDifference(existingMap.MapAgvs, map.MapAgvs, ma => ma.AgvId);

        foreach (var node in nodesToAdd)
        {
            context.Attach(node);
            existingMap.Nodes.Add(node);
        }

        foreach (var node in nodesToRemove)
        {
            existingMap.Nodes.Remove(node);
        }

        foreach (var edge in edgesToAdd)
        {
            context.Attach(edge);
            existingMap.Edges.Add(edge);
        }

        foreach (var edge in edgesToRemove)
        {
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