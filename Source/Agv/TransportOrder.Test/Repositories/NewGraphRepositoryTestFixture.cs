using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class NewGraphRepositoryTestFixture : TransportOrderDbFixture
{
    public NewGraphRepository GraphRepository { get; }

    public NewGraphRepositoryTestFixture()
    {
        GraphRepository = new NewGraphRepository(ContextFactory);
    }

    /// <summary>
    /// Creates a graph and links the given nodes/edges/stations to it via the "GraphId" shadow property
    /// (see <see cref="Persistence.Ef.DataAccess.Configuration.NewGraph.NewGraphEntityConfiguration"/>).
    /// </summary>
    public async Task<Graph> AddGraphAsync(Node[]? nodes = null, Edge[]? edges = null, Station[]? stations = null)
    {
        var graph = new Graph();

        await using var context = await ContextFactory.CreateDbContextAsync();
        await context.AddAsync(graph);

        foreach (var node in nodes ?? [])
        {
            context.Add(node).Property("GraphId").CurrentValue = graph.Id;
        }
        foreach (var edge in edges ?? [])
        {
            context.Add(edge).Property("GraphId").CurrentValue = graph.Id;
        }
        foreach (var station in stations ?? [])
        {
            context.Add(station).Property("GraphId").CurrentValue = graph.Id;
        }

        await context.SaveChangesAsync();
        return graph;
    }

    /// <summary>
    /// Adds the given routing resources (with their "ParentId" shadow property set, see
    /// <see cref="RoutingResourceEntityConfiguration"/>) in a single SaveChanges call, so that resources
    /// referencing each other (e.g. a RoutingEdge and its StartNode/EndNode) are correctly linked instead
    /// of being inserted twice.
    /// </summary>
    public async Task AddRoutingResourcesAsync(params (RoutingResource Resource, Guid ParentId)[] resources)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        foreach (var (resource, parentId) in resources)
        {
            context.Add(resource).Property(RoutingResourceEntityConfiguration.ParentId).CurrentValue = parentId;
        }
        await context.SaveChangesAsync();
    }
}
