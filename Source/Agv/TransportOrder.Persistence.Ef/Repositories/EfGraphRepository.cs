using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Repository for persistence of the graph
/// </summary>
public class EfGraphRepository : IGraphRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly IMapper _mapper;
    private readonly ISemaphoreProvider<IGraphRepositorySemaphore> _semaphoreProvider;
    private readonly ILogger<EfGraphRepository> _logger;

    /// <inheritdoc cref="EfGraphRepository" />
    public EfGraphRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory,
        IMapper mapper,
        ISemaphoreProvider<IGraphRepositorySemaphore> semaphoreProvider,
        ILogger<EfGraphRepository> logger)
    {
        _dbContextFactory = dbContextFactory;
        _mapper = mapper;
        _semaphoreProvider = semaphoreProvider;
        _logger = logger;
    }

    private static IQueryable<AgvGraph> QueryAgvGraphWithChildren(TransportOrderDbContext context)
    {
        return context.Set<AgvGraph>()
            .Include(g => g.Nodes)
            .Include(g => g.Edges)
            .AsNoTracking();
    }

    /// <inheritdoc />
    public async Task<AgvGraph> GetAgvGraphAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var agvGraph = await QueryAgvGraphWithChildren(context)
            .SingleOrDefaultAsync(g => g.AgvCorrelationId == agvCorrelationId);

        return agvGraph ?? throw new AgvGraphNotDefinedException(agvCorrelationId);
    }

    /// <inheritdoc />
    public async Task<ICollection<Guid>> GetAgvsRelatedToMap(Guid mapId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await QueryAgvGraphWithChildren(context)
            .Select(g => g.AgvCorrelationId)
            .Distinct()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ICollection<AgvGraph>> GetSelectedAgvGraphsAsync(ICollection<Guid> correlationIds)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<AgvGraph> agvGraphs = await QueryAgvGraphWithChildren(context)
            .Where(g => correlationIds.Contains(g.AgvCorrelationId))
            .ToListAsync();

        return agvGraphs;
    }

    /// <inheritdoc />
    public async Task<bool> HasAnyNodeOfTypeAsync(Guid agvCorrelationId, AgvGraph.NodeType nodeType)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        bool exists = await context.Set<AgvGraph>()
            .Include(g => g.Nodes)
            .AsNoTracking()
            .AnyAsync(g => g.AgvCorrelationId == agvCorrelationId && g.Nodes.Any(n => ((string)n.Type).Contains(nodeType.ToString())));

        return exists;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GraphNode>> GetNodesAsync(Guid agvCorrelationId, Expression<Func<GraphNode, bool>>? predicate = null)
    {
        var graph = await GetAgvGraphAsync(agvCorrelationId);
        Func<GraphNode, bool>? filter = predicate?.Compile();

        IEnumerable<GraphNode> agvNodes = graph.Nodes.Where(n => filter?.Invoke(n) ?? true);

        return await GetNodesByIdsAsync(agvNodes.Select(a => a.Id).ToList());
    }

    /// <inheritdoc />
    public async Task<ICollection<GraphNode>> GetNodesByIdsAsync(ICollection<Guid> nodeIds)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var data = await context.Set<GraphNode>()
            .AsNoTracking()
            .LeftJoin(context.Set<NodeLock>().AsNoTracking(),
                n => n.Id,
                nl => nl.NodeId,
                (n, nl) => new { nl, n })
            .Where(t => nodeIds.Contains(t.n.Id))
            .Select(t => new { t.nl, t.n })
            .ToListAsync();

        return data.Select(x =>
        {
            x.n.Lock = x.nl;
            return x.n;
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<GraphNode> GetNodeAsync(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var data = await context.Set<GraphNode>()
            .AsNoTracking()
            .LeftJoin(context.Set<NodeLock>().AsNoTracking(),
                n => n.Id,
                nl => nl.NodeId,
                (n, nl) => new { nl, n })
            .Where(t => t.n.Id == id)
            .Select(t => new { t.nl, t.n })
            .ToListAsync();

        var node = data.Select(x =>
        {
            x.n.Lock = x.nl;
            return x.n;
        }).SingleOrDefault();

        if (node == null)
        {
            throw new NodeNotFoundException(id);
        }
        return node;
    }

    /// <inheritdoc />
    public async Task<GraphNode?> TryGetNodeByIdAsync(Guid nodeId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var data = await context.Set<GraphNode>()
            .AsNoTracking()
            .LeftJoin(context.Set<NodeLock>().AsNoTracking(),
                n => n.Id,
                nl => nl.NodeId,
                (n, nl) => new { nl, n })
            .Where(t => t.n.Id == nodeId)
            .Select(t => new { t.nl, t.n })
            .ToListAsync();

        return data.Select(x =>
        {
            x.n.Lock = x.nl;
            return x.n;
        }).SingleOrDefault();
    }

    /// <inheritdoc />
    public async Task<IGraph> GetGraphForMap(Guid mapCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        Guid[] nodeIdsUsedByGraphs = await context.Set<GraphNode>()
            .Select(x => x.Id)
            .Distinct()
            .ToArrayAsync();

        Guid[] edgeIdsUsedByGraphs = await context.Set<GraphEdge>()
            .Select(x => x.Id)
            .Distinct()
            .ToArrayAsync();

        var map = await context.Set<Map>()
            .AsNoTracking()
            .Where(m => m.CorrelationId == mapCorrelationId)
            .Include(m => m.Nodes
                .Where(n => ((IEnumerable<Guid>)nodeIdsUsedByGraphs).Contains(n.Id)))
            .Include(m => m.Edges
                .Where(e => ((IEnumerable<Guid>)edgeIdsUsedByGraphs).Contains(e.Id)))
            .SingleOrDefaultAsync();

        if (map == null)
        {
            throw new MapNotFoundException(mapCorrelationId);
        }

        var graph = new Graph
        {
            Nodes = _mapper.Map<List<GraphNode>>(map.Nodes),
            Edges = _mapper.Map<List<GraphEdge>>(map.Edges)
        };

        return graph;
    }

    /// <inheritdoc />
    public Task<(bool AnyRelationsMissing, bool AnyChanges)> AddOrUpdateAgvGraphAsync(AgvGraph agvGraph)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            int existingNodesCount = await context.Set<GraphNode>().CountAsync(node => agvGraph.Nodes.Select(n => n.Id).Contains(node.Id));
            if (existingNodesCount < agvGraph.Nodes.Count)
            {
                List<string> missingNodes = agvGraph.Nodes.Select(n => n.Name).Except(await context.Set<GraphNode>().Select(n => n.Name).ToListAsync()).ToList();
                _logger.LogWarning("Not all nodes exist yet, so we cannot store the AgvGraph yet. Missing nodes: {MissingNodes}", string.Join(", ", missingNodes));

                return (true, false);
            }

            int existingEdgesCount = await context.Set<GraphEdge>().CountAsync(edge => agvGraph.Edges.Select(e => e.Id).Contains(edge.Id));
            if (existingEdgesCount < agvGraph.Edges.Count)
            {
                List<Guid> missingEdges = agvGraph.Edges.Select(e => e.Id).Except(await context.Set<GraphEdge>().Select(e => e.Id).ToListAsync()).ToList();
                _logger.LogWarning("Not all edges exist yet, so we cannot store the AgvGraph yet. Missing edges: {MissingEdges}", string.Join(", ", missingEdges));
                return (true, false);
            }

            context.AttachRange(agvGraph.Nodes);
            context.AttachRange(agvGraph.Edges);

            var existingGraph = await context.Set<AgvGraph>()
                .Include(g => g.Nodes)
                .Include(g => g.Edges)
                .SingleOrDefaultAsync(g => g.AgvCorrelationId == agvGraph.AgvCorrelationId);

            if (existingGraph == null)
            {
                context.Add((object)agvGraph);
            }
            else
            {
                existingGraph.Usability = agvGraph.Usability;

                existingGraph.Nodes.Clear();
                foreach (var node in agvGraph.Nodes)
                {
                    existingGraph.Nodes.Add(node);
                }

                existingGraph.Edges.Clear();
                foreach (var edge in agvGraph.Edges)
                {
                    existingGraph.Edges.Add(edge);
                }
            }

            int changedEntries = await context.SaveChangesAsync();
            return (false, changedEntries > 0);
        });
    }

    /// <inheritdoc />
    public async Task RemoveAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var graph = await context.Set<AgvGraph>()
            .AsNoTracking()
            .SingleOrDefaultAsync(g => g.AgvCorrelationId == agvCorrelationId);

        if (graph == null)
        {
            throw new AgvGraphNotDefinedException(agvCorrelationId);
        }
        context.Remove(graph);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        bool exists = await context.Set<AgvGraph>()
            .AsNoTracking()
            .AnyAsync(g => g.AgvCorrelationId == agvCorrelationId);

        return exists;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        bool exists = await context.Set<GraphNode>()
            .AsNoTracking()
            .AnyAsync();

        return exists;
    }

    /// <inheritdoc />
    public async Task<bool> NodeExistsByIdAsync(Guid nodeId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        bool exists = await context.Set<GraphNode>()
            .AsNoTracking()
            .AnyAsync(n => n.Id == nodeId);

        return exists;
    }

    /// <inheritdoc />
    public async Task ThrowIfNodeNotExistsAsync(Guid? nodeId)
    {
        GraphNode? node = null;
        if (nodeId.HasValue)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            node = await context.Set<GraphNode>()
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == nodeId);
        }

        if (node == null)
        {
            throw new NodeNotFoundException(nodeId);
        }
    }

    /// <inheritdoc />
    public async Task<bool> EdgeExistsAsync(Guid edgeId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Set<GraphEdge>()
            .AsNoTracking()
            .AnyAsync(e => e.Id == edgeId);
    }

    /// <inheritdoc />
    public async Task<Graph> GetGraphAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<GraphNode> efNodes = await context.Set<GraphNode>()
            .AsNoTracking()
            .ToListAsync();

        List<GraphEdge> efEdges = await context.Set<GraphEdge>()
            .AsNoTracking()
            .ToListAsync();

        var graph = new Graph
        {
            Edges = _mapper.Map<List<GraphEdge>>(efEdges),
            Nodes = _mapper.Map<List<GraphNode>>(efNodes)
        };

        return graph;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GraphNode>> GetNodesAsync(Expression<Func<GraphNode, bool>>? predicate = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<GraphNode> query = context.Set<GraphNode>()
            .AsNoTracking();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        List<GraphNode> graphNodes = await query.ToListAsync();
        List<Guid> nodeIds = graphNodes.Select(n => n.Id).ToList();

        List<NodeLock> nodeLocks = await context.Set<NodeLock>()
            .Where(x => nodeIds.Contains(x.NodeId))
            .ToListAsync();
        graphNodes.ForEach(gn => gn.Lock = nodeLocks.SingleOrDefault(nl => nl.NodeId == gn.Id));
        return graphNodes;
    }

    /// <inheritdoc />
    public Task UpdateGraphAsync(Graph graph)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var efNodes = _mapper.Map<List<GraphNode>>(graph.Nodes);
            var efEdges = _mapper.Map<List<GraphEdge>>(graph.Edges);

            List<GraphNode> existingNodes = await context.Set<GraphNode>()
                .Where(efNode => graph.Nodes.Select(n => n.Id).Contains(efNode.Id))
                .ToListAsync();

            List<GraphNode> newNodes = efNodes
                .Where(n => existingNodes.All(en => en.Id != n.Id))
                .ToList();
            List<GraphNode> updateNodes = efNodes
                .Where(n => existingNodes.Any(en => en.Id == n.Id))
                .ToList();

            context.AddRange(newNodes);

            foreach (var incoming in updateNodes)
            {
                var tracked = existingNodes.Single(en => en.Id == incoming.Id);

                context.Entry(tracked).CurrentValues.SetValues(incoming);
            }

            List<GraphEdge> existingEdges = await context.Set<GraphEdge>()
                .Where(efEdge => graph.Edges.Select(e => e.Id).Contains(efEdge.Id))
                .ToListAsync();

            List<GraphEdge> newEdges = efEdges
                .Where(e => existingEdges.All(ee => ee.Id != e.Id))
                .ToList();
            List<GraphEdge> updateEdges = efEdges
                .Where(e => existingEdges.Any(ee => ee.Id == e.Id))
                .ToList();

            context.AddRange(newEdges);

            foreach (var incoming in updateEdges)
            {
                var tracked = existingEdges.Single(ee => ee.Id == incoming.Id);
                context.Entry(tracked).CurrentValues.SetValues(incoming);
            }

            await context.SaveChangesAsync();
        }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<ICollection<Guid>> GetAgvIdsRelatedToNodesAsync(ICollection<GraphNode> nodes)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var efNodes = _mapper.Map<List<GraphNode>>(nodes);

        // get EfAgvGraph which have at least one node in the given list
        List<Guid> agvIds = await context.Set<AgvGraph>()
            .Where(agvGraph => agvGraph.Nodes.Any(efNode => efNodes.Select(n => n.Id).Contains(efNode.Id)))
            .Select(agvGraph => agvGraph.AgvCorrelationId)
            .Distinct()
            .ToListAsync();

        return agvIds;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, GraphNode?>> GetLastNodesForAgvsAsync(Dictionary<Guid, Guid?> lastNodeIdsByAgvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<GraphNode> nodeContext = context.Set<GraphNode>();
        List<GraphNode> nodeList = await nodeContext.Where(n => lastNodeIdsByAgvCorrelationId.Select(l => l.Value).Contains(n.Id))
            .AsNoTracking()
            .ToListAsync();
        Dictionary<Guid, GraphNode?> nodesByAgv = new();
        foreach (KeyValuePair<Guid, Guid?> element in lastNodeIdsByAgvCorrelationId)
        {
            var efNode = nodeList.SingleOrDefault(pn => pn.Id == element.Value);
            nodesByAgv.Add(element.Key, _mapper.Map<GraphNode>(efNode));
        }
        return nodesByAgv;
    }

    /// <inheritdoc />
    public async Task UpdateNode(GraphNode node)
    {
        await _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var efNode = _mapper.Map<GraphNode>(node);

            context.Update(efNode);
            await context.SaveChangesAsync();
        });
    }

    private const string GraphConsolidationSavePoint = "BeforeGraphConsolidation";

    /// <inheritdoc />
    public async Task<bool> ConsolidateGraphAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (transaction.SupportsSavepoints)
            {
                await transaction.CreateSavepointAsync(GraphConsolidationSavePoint);
            }
            List<AgvGraph> agvGraphs = await QueryAgvGraphWithChildren(context).ToListAsync();
            HashSet<Guid> usedNodes = agvGraphs.SelectMany(g => g.Nodes.Select(n => n.Id)).ToHashSet();
            HashSet<Guid> usedEdges = agvGraphs.SelectMany(g => g.Edges.Select(n => n.Id)).ToHashSet();

            await RemoveStaleMapReferencesAsync(context, usedNodes, usedEdges);
            await AbortAndCleanActionsWithStaleNodesAsync(context, usedNodes);
            await context.SaveChangesAsync();

            IQueryable<GraphNode> unusedNodes = context.Set<GraphNode>().Where(n => !usedNodes.Contains(n.Id));
            IQueryable<GraphEdge> unusedEdges = context.Set<GraphEdge>().Where(e => !usedEdges.Contains(e.Id));
            IQueryable<Station> unusedStations = context.Set<Station>().Where(s => !usedNodes.Contains(s.Id));
            IQueryable<NodeLock> unusedNodeLocks = context.Set<NodeLock>().Where(n => !usedNodes.Contains(n.NodeId));

            context.Set<GraphNode>().RemoveRange(unusedNodes);
            context.Set<GraphEdge>().RemoveRange(unusedEdges);
            context.Set<Station>().RemoveRange(unusedStations);
            context.Set<NodeLock>().RemoveRange(unusedNodeLocks);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception)
        {
            _logger.LogError("Consolidating global graph failed, roll back transaction");
            if (transaction.SupportsSavepoints)
            {
                await transaction.RollbackToSavepointAsync(GraphConsolidationSavePoint);
                return false;
            }
            await transaction.RollbackAsync();
            return false;
        }
    }

    private static async Task RemoveStaleMapReferencesAsync(
        TransportOrderDbContext context,
        HashSet<Guid> usedNodes,
        HashSet<Guid> usedEdges)
    {
        List<Map> mapsWithStaleRefs = await context.Set<Map>()
            .Where(m => m.Nodes.Any(n => !usedNodes.Contains(n.Id))
                        || m.Edges.Any(e => !usedEdges.Contains(e.Id)))
            .Include(m => m.Nodes.Where(n => !usedNodes.Contains(n.Id)))
            .Include(m => m.Edges.Where(e => !usedEdges.Contains(e.Id)))
            .ToListAsync();

        foreach (var map in mapsWithStaleRefs)
        {
            foreach (var node in map.Nodes.ToList())
            {
                map.Nodes.Remove(node);
            }
            foreach (var edge in map.Edges.ToList())
            {
                map.Edges.Remove(edge);
            }
        }
    }

    private async Task AbortAndCleanActionsWithStaleNodesAsync(
        TransportOrderDbContext context,
        HashSet<Guid> usedNodes)
    {
        var terminalStates = new[] { ActionState.Completed, ActionState.Aborted, ActionState.Canceled, ActionState.Invalid };
        var abortReason = new Reason(Reasons.ActionGroupAbortedDueToGraphChange);
        var now = DateTimeOffset.UtcNow;

        List<ActionGroup> affectedGroups = await context.Set<ActionGroup>()
            .Where(g => g.Actions.Any(a =>
                a.AgvActionNodes.Any(n => !usedNodes.Contains(n.NodeId)) ||
                (a.Target.NodeId != null && !usedNodes.Contains(a.Target.NodeId.Value))))
            .ToListAsync();

        foreach (var group in affectedGroups)
        {
            foreach (var action in group.Actions)
            {
                bool hasStalePathNodes = action.AgvActionNodes.Any(n => !usedNodes.Contains(n.NodeId));
                bool hasStaleTarget = action.Target.NodeId != null && !usedNodes.Contains(action.Target.NodeId.Value);
                if (!hasStalePathNodes && !hasStaleTarget)
                {
                    continue;
                }

                if (!terminalStates.Contains(action.State))
                {
                    _logger.LogWarning(
                        "Aborting action {ActionId} in group {GroupId} because graph consolidation removed nodes used by this action",
                        action.CorrelationId, group.CorrelationId);
                    group.AbortAction(action.CorrelationId, [abortReason], now);
                }

                action.AgvActionNodes.Clear();

                if (hasStaleTarget)
                {
                    action.Target.Node = null;
                    action.Target.NodeId = null;
                }
            }
        }
    }
}