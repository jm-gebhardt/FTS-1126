using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Provides access to the graph from DB.
/// </summary>
/// <param name="contextFactory">The database context factory.</param>
[UsedImplicitly]
public class NewGraphRepository(IDbContextFactory<TransportOrderDbContext> contextFactory) : INewGraphRepository
{
    private static async Task<Dictionary<Guid, Node>> AddOrUpdateNodesAsync(TransportOrderDbContext context, IReadOnlyCollection<Node> nodes, Guid graphSegmentId)
    {
        List<Guid> nodeIds = nodes.Select(n => n.Id).ToList();
        Dictionary<Guid, Node> storedNodes = await context.Set<Node>()
            .Where(n => nodeIds.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id);

        foreach (var node in nodes)
        {
            if (storedNodes.TryGetValue(node.Id, out var storedNode))
            {
                context.Entry(storedNode).CurrentValues.SetValues(node);
                storedNode.GraphSegmentId = graphSegmentId;
                continue;
            }

            node.GraphSegmentId = graphSegmentId;
            context.Add(node);
            storedNodes.Add(node.Id, node);
        }

        return storedNodes;
    }

    private static async Task AddOrUpdateEdgesAsync(TransportOrderDbContext context, IReadOnlyCollection<Edge> edges, Guid graphSegmentId)
    {
        List<Guid> edgeIds = edges.Select(e => e.Id).ToList();
        Dictionary<Guid, Edge> storedEdges = await context.Set<Edge>()
            .Where(e => edgeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id);

        foreach (var edge in edges)
        {
            if (storedEdges.TryGetValue(edge.Id, out var storedEdge))
            {
                context.Entry(storedEdge).CurrentValues.SetValues(edge);
                storedEdge.GraphSegmentId = graphSegmentId;
                continue;
            }

            edge.GraphSegmentId = graphSegmentId;
            context.Add(edge);
        }
    }

    private static async Task AddOrUpdateStationsAsync(TransportOrderDbContext context, IReadOnlyCollection<Station> stations, Dictionary<Guid, Node> nodesById, Guid graphSegmentId)
    {
        List<Guid> stationIds = stations.Select(s => s.Id).ToList();
        Dictionary<Guid, Station> storedStations = await context.Set<Station>()
            .Include(s => s.InteractionNodes)
            .Where(s => stationIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        foreach (var station in stations)
        {
            List<Node> interactionNodes = station.InteractionNodeIds.Select(id => nodesById[id]).ToList();

            if (storedStations.TryGetValue(station.Id, out var storedStation))
            {
                context.Entry(storedStation).CurrentValues.SetValues(station);
                storedStation.UpdateInteractionNodes(interactionNodes);
                storedStation.GraphSegmentId = graphSegmentId;
                continue;
            }

            station.UpdateInteractionNodes(interactionNodes);
            station.GraphSegmentId = graphSegmentId;
            context.Add(station);
        }
    }

    /// <inheritdoc/>
    public async Task<Graph> GetGraphAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        List<Node> nodes = await context.Set<Node>().Include(n => n.RoutingNodes).ToListAsync();
        List<Edge> edges = await context.Set<Edge>().Include(n => n.RoutingEdges).ToListAsync();
        List<Station> stations = await context.Set<Station>().Include(s => s.InteractionNodes).ToListAsync();

        var graph = new Graph(nodes, edges, stations);

        return graph;
    }

    /// <inheritdoc/>
    public async Task AddOrUpdateGraphSegmentAsync(GraphSegment graphSegment)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var storedSegment = await context.Set<GraphSegment>().FindAsync(graphSegment.Id);

        if (storedSegment is null)
        {
            // Add an empty shell: the resources are added/updated individually below. Adding graphSegment itself would
            // attach its resources via the navigations and conflict with already stored instances.
            context.Add(new GraphSegment(graphSegment.Id, new List<Node>(), new List<Edge>(), new List<Station>()));
        }

        Dictionary<Guid, Node> nodesById = await AddOrUpdateNodesAsync(context, graphSegment.Nodes, graphSegment.Id);
        await AddOrUpdateEdgesAsync(context, graphSegment.Edges, graphSegment.Id);
        await AddOrUpdateStationsAsync(context, graphSegment.Stations, nodesById, graphSegment.Id);

        await context.SaveChangesAsync();
    }
    
    /// <inheritdoc/>
    public async Task<bool> DeleteGraphSegmentAsync(Guid graphSegmentId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var storedSegment = await context.Set<GraphSegment>()
            .Include(s => s.Nodes)
            .Include(s => s.Edges)
            .Include(s => s.Stations).ThenInclude(s => s.InteractionNodes)
            .AsSplitQuery()
            .SingleOrDefaultAsync(s => s.Id == graphSegmentId);

        if (storedSegment is null)
        {
            return false;
        }

        // Nodes, edges and stations are deleted by ClientCascade (see GraphSegmentEntityConfiguration).
        context.Remove(storedSegment);

        await context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> RoutingNodeExistsAsync(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.Set<RoutingNode>().AsNoTracking().AnyAsync(n => n.Id == id);
    }

    /// <inheritdoc/>
    public async Task<bool> RoutingEdgeExistsAsync(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.Set<RoutingEdge>().AsNoTracking().AnyAsync(e => e.Id == id);
    }
}