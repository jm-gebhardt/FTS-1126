using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Models;
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
    private static async Task<Dictionary<Guid, Node>> AddOrUpdateNodesAsync(TransportOrderDbContext context, IReadOnlyCollection<Node> nodes)
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

                continue;
            }

            context.Add(node);
            storedNodes.Add(node.Id, node);
        }

        return storedNodes;
    }

    private static async Task AddOrUpdateEdgesAsync(TransportOrderDbContext context, IReadOnlyCollection<Edge> edges, Dictionary<Guid, Node> nodesById)
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

                continue;
            }

            context.Add(new Edge(edge.Id, edge.Name, edge.ShortName, nodesById[edge.StartNodeId], nodesById[edge.EndNodeId]));
        }
    }

    private static async Task AddOrUpdateStationsAsync(TransportOrderDbContext context, IReadOnlyCollection<Station> stations, Dictionary<Guid, Node> nodesById)
    {
        List<Guid> stationIds = stations.Select(s => s.Id).ToList();
        Dictionary<Guid, Station> storedStations = await context.Set<Station>()
            .Include(s => s.InteractionNodes)
            .Where(s => stationIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);

        foreach (var station in stations)
        {
            List<Node> interactionNodes = station.InteractionNodes.Select(n => nodesById[n.Id]).ToList();

            if (storedStations.TryGetValue(station.Id, out var storedStation))
            {
                context.Entry(storedStation).CurrentValues.SetValues(station);
                storedStation.InteractionNodes.Clear();
                storedStation.InteractionNodes.AddRange(interactionNodes);

                continue;
            }

            var newStation = new Station(station.Id, station.Name, station.ShortName, station.StationHeight, station.X, station.Y, station.Theta);
            newStation.InteractionNodes.AddRange(interactionNodes);
            context.Add(newStation);
        }
    }

    private static async Task LoadRoutingResourcesAsync(TransportOrderDbContext context, Graph graph)
    {
        Dictionary<Guid, GraphResource> parentsById = new List<GraphResource>()
            .Concat(graph.Nodes)
            .Concat(graph.Edges)
            .ToDictionary(p => p.Id);
        List<Guid> parentIds = parentsById.Keys.ToList();

        List<RoutingResource> routingResources = await context.Set<RoutingResource>()
            .Include(r => r.AgvType)
            .Include(r => ((RoutingEdge)r).StartNode)
            .Include(r => ((RoutingEdge)r).EndNode)
            .Include(r => ((RoutingNode)r).IncomingEdges)
            .Include(r => ((RoutingNode)r).OutgoingEdges)
            // ReSharper disable once EntityFramework.ClientSideDbFunctionCall
            .Where(r => parentIds.Contains(EF.Property<Guid>(r, RoutingResourceEntityConfiguration.ParentId)))
            .AsSplitQuery()
            .ToListAsync();

        foreach (var routingResource in routingResources)
        {
            var parentId = context.Entry(routingResource).Property<Guid>(RoutingResourceEntityConfiguration.ParentId).CurrentValue;
            if (parentsById.TryGetValue(parentId, out var parent))
            {
                routingResource.AttachParent(parent);
                parent.AddChild(routingResource);
            }
        }

        Dictionary<Guid, RoutingResource> routingResourcesById = routingResources.ToDictionary(r => r.Id);
        List<Guid> resourceIds = routingResourcesById.Keys.ToList();

        List<RoutingResourceConflict> conflicts = await context.Set<RoutingResourceConflict>()
            .Where(c => resourceIds.Contains(c.ResourceId) || resourceIds.Contains(c.ConflictingResourceId))
            .ToListAsync();

        foreach (var conflict in conflicts)
        {
            if (routingResourcesById.TryGetValue(conflict.ResourceId, out var resource) &&
                routingResourcesById.TryGetValue(conflict.ConflictingResourceId, out var conflictingResource))
            {
                resource.AddConflictingResource(conflictingResource);
                conflictingResource.AddConflictingResource(resource);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<Graph> GetGraphAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var graph = new Graph();
        graph.Nodes.AddRange(await context.Set<Node>().ToListAsync());
        graph.Edges.AddRange(await context.Set<Edge>().ToListAsync());
        graph.Stations.AddRange(await context.Set<Station>().Include(s => s.InteractionNodes).ToListAsync());

        await LoadRoutingResourcesAsync(context, graph);

        return graph;
    }

    /// <inheritdoc/>
    public async Task AddOrUpdateGraphAsync(Graph graph)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        Dictionary<Guid, Node> nodesById = await AddOrUpdateNodesAsync(context, graph.Nodes);
        await AddOrUpdateEdgesAsync(context, graph.Edges, nodesById);
        await AddOrUpdateStationsAsync(context, graph.Stations, nodesById);

        await context.SaveChangesAsync();
    }
}