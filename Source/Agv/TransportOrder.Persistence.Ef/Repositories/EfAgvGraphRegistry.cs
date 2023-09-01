using System.Linq.Expressions;
using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Microsoft.EntityFrameworkCore;
using TransportOrder.Domain.Exceptions;
using TransportOrder.Domain.Models.Graph;
using TransportOrder.Domain.Repositories;
using TransportOrder.Persistence.Ef.DataAccess;

namespace TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Registry for persisting the AgvGraph
/// </summary>
public class EfAgvGraphRegistry : IAgvGraphRegistry
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="EfAgvGraphRegistry" />
    /// <param name="dbContextFactory"></param>
    /// <param name="mapper"></param>
    public EfAgvGraphRegistry(IDbContextFactory<TransportOrderDbContext> dbContextFactory, IMapper mapper)
    {
        _dbContextFactory = dbContextFactory;
        _mapper = mapper;
    }

    private static IQueryable<EfGraph> QueryWithChildren(TransportOrderDbContext context)
    {
        return context.Set<EfGraph>()
            .Include(g => g.Nodes)
            .Include(g => g.Edges)
            .AsSplitQuery()
            .AsNoTracking();
    }

    /// <inheritdoc />
    public async Task<AgvGraph> GetAgvGraphAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var efGraph = await QueryWithChildren(context)
            .SingleOrDefaultAsync(g => g.CorrelationId == agvCorrelationId);

        if (efGraph == null)
        {
            throw new AgvGraphNotDefinedException(agvCorrelationId);
        }
        var agvGraph = _mapper.Map<AgvGraph>(efGraph);

        return agvGraph;
    }

    /// <inheritdoc />
    public async Task<List<AgvGraph>> GetAllAgvGraphsAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<EfGraph> efGraphs = await QueryWithChildren(context)
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
    public async Task<GraphNode> GetNodeAsync(string id, Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var node = await context.Set<EfNode>()
            .Where(n => n.GraphCorrelationId == agvCorrelationId && n.Id == id)
            .AsNoTracking()
            .SingleOrDefaultAsync();

        if (node == null)
        {
            throw new NodeNotFoundException(agvCorrelationId, "lastNode was not found");
        }

        var graphNode = _mapper.Map<GraphNode>(node);

        return graphNode;
    }

    /// <inheritdoc />
    public async Task AddOrUpdateAsync(AgvGraph agvGraph)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var efGraph = _mapper.Map<EfGraph>(agvGraph);

        var existingGraph = await context.Set<EfGraph>()
            .AsNoTracking()
            .SingleOrDefaultAsync(g => g.CorrelationId == agvGraph.CorrelationId);

        if (existingGraph == null)
        {
            await context.AddAsync(efGraph);
        }
        else
        {
            // For an existing graph, just overwrite everything, we do not have to check anything
            context.Entry(existingGraph).CurrentValues.SetValues(efGraph);
        }

        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task RemoveAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var graph = await context.Set<EfGraph>()
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

        bool exists = await context.Set<EfGraph>()
            .AsNoTracking()
            .AnyAsync(g => g.CorrelationId == agvCorrelationId);

        return exists;
    }
}