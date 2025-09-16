using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;

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
    /// <param name="dbContextFactory"></param>
    /// <param name="mapper"></param>
    /// <param name="logger"></param>
    /// <param name="semaphoreProvider"></param>
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

    private static IQueryable<EfAgvGraph> QueryAgvGraphWithChildren(TransportOrderDbContext context)
    {
        return context.Set<EfAgvGraph>()
            .Include(g => g.Nodes)
            .Include(g => g.Edges)
            .AsSplitQuery()
            .AsNoTracking();
    }

    /// <inheritdoc />
    public async Task<AgvGraph> GetAgvGraphAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var efGraph = await QueryAgvGraphWithChildren(context)
            .SingleOrDefaultAsync(g => g.CorrelationId == agvCorrelationId);

        if (efGraph == null)
        {
            throw new AgvGraphNotDefinedException(agvCorrelationId);
        }
        var agvGraph = _mapper.Map<AgvGraph>(efGraph);

        return agvGraph;
    }

    /// <inheritdoc />
    public async Task<ICollection<Guid>> GetAgvsRelatedToMap(Guid mapId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await QueryAgvGraphWithChildren(context)
            .Select(g => g.CorrelationId)
            .Distinct()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ICollection<AgvGraph>> GetSelectedAgvGraphsAsync(ICollection<Guid> correlationIds)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<EfAgvGraph> efGraphs = await QueryAgvGraphWithChildren(context)
            .Where(g => correlationIds.Contains(g.CorrelationId))
            .ToListAsync();

        var agvGraphs = _mapper.Map<List<AgvGraph>>(efGraphs);

        return agvGraphs;
    }

    /// <inheritdoc />
    public async Task<bool> HasAnyNodeOfTypeAsync(Guid agvCorrelationId, AgvGraph.NodeType nodeType)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        bool exists = await context.Set<EfAgvGraph>()
            .Include(g => g.Nodes)
            .AsNoTracking()
            .AnyAsync(g => g.CorrelationId == agvCorrelationId && g.Nodes.Any(n => ((string) n.Type).Contains(nodeType.ToString())));

        return exists;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GraphNode>> GetNodesAsync(Guid agvCorrelationId, Expression<Func<GraphNode, bool>>? predicate = null)
    {
        var graph = await GetAgvGraphAsync(agvCorrelationId);
        Func<GraphNode, bool>? filter = predicate?.Compile();

        IEnumerable<GraphNode> nodes = graph.Nodes.Where(n => filter?.Invoke(n) ?? true);

        return nodes;
    }

    /// <inheritdoc />
    public async Task<ICollection<GraphNode>> GetNodesByIdsAsync(ICollection<Guid> nodeIds)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<EfNode> nodes = await context.Set<EfAgvGraph>()
            .Include(g => g.Nodes)
            .SelectMany(g => g.Nodes)
            .Where(n => nodeIds.Contains(n.Id))
            .AsNoTracking()
            .ToListAsync();
        return _mapper.Map<List<GraphNode>>(nodes);
    }

    /// <inheritdoc />
    public async Task<GraphNode> GetNodeAsync(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var node = await context.Set<EfNode>()
            .Where(n => n.Id == id)
            .AsNoTracking()
            .SingleOrDefaultAsync();

        if (node == null)
        {
            throw new NodeNotFoundException(id);
        }

        var graphNode = _mapper.Map<GraphNode>(node);

        return graphNode;
    }

    /// <inheritdoc />
    public async Task<GraphNode?> TryGetNodeByIdAsync(Guid nodeId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var node = await context.Set<EfNode>()
            .Where(n => n.Id == nodeId)
            .AsNoTracking()
            .SingleOrDefaultAsync();

        var graphNode = _mapper.Map<GraphNode?>(node);

        return graphNode;
    }

    /// <inheritdoc />
    public async Task<IGraph> GetGraphForMap(Guid mapCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var map = await context.Set<Map>()
            .AsNoTracking()
            .AsSplitQuery()
            .Where(m => m.CorrelationId == mapCorrelationId)
            // Only include nodes/edges which are attached to at least one AgvGraph
            .Include(m => m.Nodes
                .Where(n => n.AgvGraphs.Count > 0))
            .Include(m => m.Edges
                .Where(e => e.AgvGraphs.Count > 0))
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

            var efGraph = _mapper.Map<EfAgvGraph>(agvGraph);

            int existingNodesCount = await context.Set<EfNode>().CountAsync(node => efGraph.Nodes.Select(n => n.Id).Contains(node.Id));
            if (existingNodesCount < efGraph.Nodes.Count)
            {
                List<string> missingNodes = efGraph.Nodes.Select(n => n.Name).Except(await context.Set<EfNode>().Select(n => n.Name).ToListAsync()).ToList();
                _logger.LogWarning("Not all nodes exist yet, so we cannot store the AgvGraph yet. Missing nodes: {MissingNodes}", string.Join(", ", missingNodes));

                return (true, false);
            }

            int existingEdgesCount = await context.Set<EfEdge>().CountAsync(edge => efGraph.Edges.Select(e => e.Id).Contains(edge.Id));
            if (existingEdgesCount < efGraph.Edges.Count)
            {
                List<Guid> missingEdges = efGraph.Edges.Select(e => e.Id).Except(await context.Set<EfEdge>().Select(e => e.Id).ToListAsync()).ToList();
                _logger.LogWarning("Not all edges exist yet, so we cannot store the AgvGraph yet. Missing edges: {MissingEdges}", string.Join(", ", missingEdges));
                return (true, false);
            }

            context.AttachRange(efGraph.Nodes);
            context.AttachRange(efGraph.Edges);

            var existingGraph = await context.Set<EfAgvGraph>()
                .Include(g => g.Nodes)
                .Include(g => g.Edges)
                .SingleOrDefaultAsync(g => g.CorrelationId == agvGraph.CorrelationId);

            if (existingGraph == null)
            {
                context.Add((object) efGraph);
            }
            else
            {
                existingGraph.Usability = efGraph.Usability;

                existingGraph.Nodes.Clear();
                foreach (var node in efGraph.Nodes)
                {
                    existingGraph.Nodes.Add(node);
                }

                existingGraph.Edges.Clear();
                foreach (var edge in efGraph.Edges)
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

        var graph = await context.Set<EfAgvGraph>()
            .AsNoTracking()
            .SingleOrDefaultAsync(g => g.CorrelationId == agvCorrelationId);

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

        bool exists = await context.Set<EfAgvGraph>()
            .AsNoTracking()
            .AnyAsync(g => g.CorrelationId == agvCorrelationId);

        return exists;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        bool exists = await context.Set<EfNode>()
            .AsNoTracking()
            .AnyAsync();

        return exists;
    }

    /// <inheritdoc />
    public async Task<bool> NodeExistsByIdAsync(Guid nodeId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        bool exists = await context.Set<EfNode>()
            .AsNoTracking()
            .AnyAsync(n => n.Id == nodeId);

        return exists;
    }

    /// <inheritdoc />
    public async Task ThrowIfNodeNotExistsAsync(Guid? nodeId)
    {
        EfNode? node = null;
        if (nodeId.HasValue)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            node = await context.Set<EfNode>()
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

        return await context.Set<EfEdge>()
            .AsNoTracking()
            .AnyAsync(e => e.Id == edgeId);
    }

    /// <inheritdoc />
    public async Task<Graph> GetGraphAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<EfNode> efNodes = await context.Set<EfNode>()
            .AsNoTracking()
            .ToListAsync();

        List<EfEdge> efEdges = await context.Set<EfEdge>()
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
    public async Task<IEnumerable<GraphNode>> GetNodesAsync(Expression<Func<EfNode, bool>>? predicate = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<EfNode> query = context.Set<EfNode>()
            .AsNoTracking();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        List<EfNode> efNodes = await query.ToListAsync();
        var graphNodes = _mapper.Map<List<GraphNode>>(efNodes);

        return graphNodes;
    }

    /// <inheritdoc />
    public Task UpdateGraphAsync(Graph graph)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var efNodes = _mapper.Map<List<EfNode>>(graph.Nodes);
            var efEdges = _mapper.Map<List<EfEdge>>(graph.Edges);

            List<EfNode> existingNodes = await context.Set<EfNode>()
                .Where(efNode => graph.Nodes.Select(n => n.Id).Contains(efNode.Id))
                .ToListAsync();

            List<EfNode> newNodes = efNodes
                .Where(n => existingNodes.All(en => en.Id != n.Id))
                .ToList();
            List<EfNode> updateNodes = efNodes
                .Where(n => existingNodes.Any(en => en.Id == n.Id))
                .ToList();

            context.AddRange(newNodes);

            foreach (var incoming in updateNodes)
            {
                var tracked = existingNodes.Single(en => en.Id == incoming.Id);

                context.Entry(tracked).CurrentValues.SetValues(incoming);
            }

            List<EfEdge> existingEdges = await context.Set<EfEdge>()
                .Where(efEdge => graph.Edges.Select(e => e.Id).Contains(efEdge.Id))
                .ToListAsync();

            List<EfEdge> newEdges = efEdges
                .Where(e => existingEdges.All(ee => ee.Id != e.Id))
                .ToList();
            List<EfEdge> updateEdges = efEdges
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
        var efNodes = _mapper.Map<List<EfNode>>(nodes);

        // get EfAgvGraph which have at least one node in the given list
        List<Guid> agvIds = await context.Set<EfAgvGraph>()
            .Where(agvGraph => agvGraph.Nodes.Any(efNode => efNodes.Select(n => n.Id).Contains(efNode.Id)))
            .Select(agvGraph => agvGraph.CorrelationId)
            .Distinct()
            .ToListAsync();

        return agvIds;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, GraphNode?>> GetLastNodesForAgvsAsync(Dictionary<Guid, Guid?> lastNodeIdsByAgvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<EfNode> nodeContext = context.Set<EfNode>();
        List<EfNode> nodeList = await nodeContext.Where(n => lastNodeIdsByAgvCorrelationId.Select(l => l.Value).Contains(n.Id))
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
            var efNode = _mapper.Map<EfNode>(node);

            context.Update(efNode);
            await context.SaveChangesAsync();
        });
    }
}