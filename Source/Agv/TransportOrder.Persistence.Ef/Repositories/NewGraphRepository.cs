using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Models;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
///
/// </summary>
/// <param name="contextFactory"></param>
[UsedImplicitly]
public class NewGraphRepository(IDbContextFactory<TransportOrderDbContext> contextFactory) :
    EfRepository<Graph, TransportOrderDbContext, IDbContextFactory<TransportOrderDbContext>>(contextFactory)
{
    /// <summary>
    /// Loads a graph with its nodes, edges and stations, each one's routing resources,
    /// and conflicting resources.
    /// </summary>
    /// <param name="id">Id of the graph to load.</param>
    [UsedImplicitly]
    public async Task<Graph?> GetGraphById(Guid id)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();

        var graph = await context.Set<Graph>()
            .Include(g => g.Nodes)
            .Include(g => g.Edges)
            .Include(g => g.Stations)
            .AsSplitQuery()
            .SingleOrDefaultAsync(g => g.Id == id);

        if (graph is null)
        {
            return null;
        }

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

        return graph;
    }
}
