using System.Linq.Expressions;
using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Map = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map.Map;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

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
    public async Task<IEnumerable<GraphNode>> GetNodesAsync(Guid agvCorrelationId, Expression<Func<GraphNode, bool>>? predicate = null)
    {
        var graph = await GetAgvGraphAsync(agvCorrelationId);
        Func<GraphNode, bool>? filter = predicate?.Compile();

        IEnumerable<GraphNode> nodes = graph.Nodes.Where(n => filter?.Invoke(n) ?? true);

        return nodes;
    }

    /// <inheritdoc />
    public async Task<GraphNode> GetNodeAsync(string id)
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
    public async Task<GraphNode?> TryGetNodeAsync(string id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var node = await context.Set<EfNode>()
            .Where(n => n.Id == id)
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
                .Include(m => m.Nodes)
                .Include(m => m.Edges)
                .AsSplitQuery()
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.CorrelationId == mapCorrelationId)
            ;

        if (map == null)
        {
            throw new MapNotFoundException();
        }

        var graph = new Graph
        {
            Nodes = _mapper.Map<List<GraphNode>>(map.Nodes),
            Edges = _mapper.Map<List<GraphEdge>>(map.Edges)
        };

        return graph;
    }

    /// <inheritdoc />
    public Task<bool> AddOrUpdateAgvGraphAsync(AgvGraph agvGraph)
    {
        return _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var efGraph = _mapper.Map<EfAgvGraph>(agvGraph);

            var existingGraph = await context.Set<EfAgvGraph>()
                .SingleOrDefaultAsync(g => g.CorrelationId == agvGraph.CorrelationId);

            if (await context.Set<EfNode>()
                    .Where(node => efGraph.Nodes.Select(n => n.Id).Contains(node.Id))
                    .CountAsync() != efGraph.Nodes.Count ||
                await context.Set<EfEdge>()
                    .Where(edge => efGraph.Edges.Select(e => e.Id).Contains(edge.Id))
                    .CountAsync() != efGraph.Edges.Count)
            {
                _logger.LogWarning("Not all nodes or edges exist yet, so we cannot store the AgvGraph yet.");
                return false;
            }

            context.AttachRange(efGraph.Nodes);
            context.AttachRange(efGraph.Edges);

            if (existingGraph == null)
            {
                context.Add((object)efGraph);
            }
            else
            {
                // For an existing graph, just overwrite the properties
                context.Update(existingGraph).CurrentValues.SetValues(efGraph);
            }

            await context.SaveChangesAsync();
            return true;
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
    public async Task<bool> NodeExistsAsync(string nodeId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        bool exists = await context.Set<EfNode>()
            .AsNoTracking()
            .AnyAsync(n => n.Id == nodeId);

        return exists;
    }

    /// <inheritdoc />
    public async Task<Graph> GetGraphAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<EfNode> efNodes = await context.Set<EfNode>()
            .ToListAsync();

        List<EfEdge> efEdges = await context.Set<EfEdge>()
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

            IEnumerable<EfNode> newNodes = efNodes.Where(efNode => !existingNodes.Select(n => n.Id).Contains(efNode.Id));
            context.AddRange(newNodes);

            List<EfEdge> existingEdges = await context.Set<EfEdge>()
                .Where(efEdge => graph.Edges.Select(n => n.Id).Contains(efEdge.Id))
                .ToListAsync();

            IEnumerable<EfEdge> newEdges = efEdges.Where(efEdge => !existingEdges.Select(n => n.Id).Contains(efEdge.Id));
            context.AddRange(newEdges);

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
    public async Task<IReadOnlyDictionary<Guid, GraphNode?>> GetLastNodesForAgvsAsync(Dictionary<Guid, string?> lastNodeIdsByAgvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<EfNode> nodeContext = context.Set<EfNode>();
        List<EfNode> nodeList = await nodeContext.Where(n => lastNodeIdsByAgvCorrelationId.Select(l => l.Value).Contains(n.Id)).AsNoTracking().ToListAsync();
        Dictionary<Guid, GraphNode?> nodesByAgv = new();
        foreach (KeyValuePair<Guid, string?> element in lastNodeIdsByAgvCorrelationId)
        {
            var efNode = nodeList.FirstOrDefault(pn => pn.Id == element.Value);
            nodesByAgv.Add(element.Key, _mapper.Map<GraphNode>(efNode));
        }
        return nodesByAgv;
    }
}