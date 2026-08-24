using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class NewGraphRepositoryTestFixture : TransportOrderDbFixture
{
    public const string NodeAName = "A";
    public const string NodeBName = "B";
    public const string NodeCName = "C";
    public const string EdgeAbName = "A-B";
    public const string EdgeAcName = "A-C";
    public const string StationName = "S";
    public const string AgvTypeName = "Still.EK1";
    public const double MovedNodePosition = 42.0;

    public NewGraphRepository GraphRepository { get; }

    public NewGraphRepositoryTestFixture()
    {
        GraphRepository = new NewGraphRepository(ContextFactory);
    }

    public static Station CreateStation(Guid id, params Node[] interactionNodes)
    {
        var station = new Station(id, StationName, null, null, 0, 0, null);
        station.InteractionNodes.AddRange(interactionNodes);

        return station;
    }

    public static Graph CreateGraph(Node[] nodes, Edge[]? edges = null, Station[]? stations = null)
    {
        var graph = new Graph();
        graph.Nodes.AddRange(nodes);
        graph.Edges.AddRange(edges ?? []);
        graph.Stations.AddRange(stations ?? []);

        return graph;
    }

    public async Task AddResourcesAsync(Node[]? nodes = null, Edge[]? edges = null, Station[]? stations = null)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();

        IEnumerable<object> resources = [.. nodes ?? [], .. edges ?? [], .. stations ?? []];
        context.AddRange(resources);

        await context.SaveChangesAsync();
    }

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