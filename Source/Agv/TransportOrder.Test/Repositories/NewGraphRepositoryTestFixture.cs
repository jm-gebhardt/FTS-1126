using Gebhardt.Acc.Common.Domain.Models.NewGraph;
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
        var station = new Station(id, StationName, StationName, 0, 0, 0, 0, interactionNodes.Select(n => n.Id).ToList());

        return station;
    }

    public static GraphSegment CreateGraph(Node[] nodes, Edge[]? edges = null, Station[]? stations = null)
    {
        var graph = new GraphSegment(nodes, edges ?? [], stations ?? []);

        return graph;
    }

    public async Task AddResourcesAsync(Node[]? nodes = null, Edge[]? edges = null, Station[]? stations = null)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();

        IEnumerable<object> resources = [.. nodes ?? [], .. edges ?? [], .. stations ?? []];
        context.AddRange(resources);

        await context.SaveChangesAsync();
    }

    public async Task AddRoutingResourcesAsync(params (RoutingResource RoutingResource, GraphResource Parent)[] resources)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        foreach (var (routingResource, parent) in resources)
        {
            string propertyName = parent switch
            {
                Node => NewNodeEntityConfiguration.NodeId,
                Edge => NewEdgeEntityConfiguration.EdgeId,
                _ => throw new InvalidOperationException($"Parent has an unsupported type: {parent.GetType().Name}")
            };

            context.Add(routingResource).Property(propertyName).CurrentValue = parent.Id;
        }
        await context.SaveChangesAsync();
    }
}