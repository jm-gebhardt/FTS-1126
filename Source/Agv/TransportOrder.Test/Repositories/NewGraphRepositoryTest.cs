using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class NewGraphRepositoryTest
{
    [TestMethod]
    public async Task GetGraphById_GraphDoesNotExist_ReturnsNull()
    {
        var fixture = new NewGraphRepositoryTestFixture();

        var result = await fixture.GraphRepository.GetGraphById(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetGraphById_GraphExists_LoadsNodesEdgesAndStationsWithTheirRoutingResourceChildren()
    {
        var fixture = new NewGraphRepositoryTestFixture();

        var nodeA = new Node(Guid.NewGuid(), "A", null, 0, 0, null);
        var nodeB = new Node(Guid.NewGuid(), "B", null, 1, 1, null);
        var edge = new Edge(Guid.NewGuid(), "A-B", null, nodeA, nodeB);
        var station = new Station(Guid.NewGuid(), "S", null, null, 0, 0, null);
        var graph = await fixture.AddGraphAsync([nodeA, nodeB], [edge], [station]);

        var otherNode = new Node(Guid.NewGuid(), "Other", null, 5, 5, null);
        await fixture.AddGraphAsync([otherNode]);

        var agvType = new AgvType(Guid.NewGuid(), "Still.EK1");
        var routingNodeA = new RoutingNode(Guid.NewGuid(), nodeA, agvType, new LoadState(false), 0);
        var routingNodeB = new RoutingNode(Guid.NewGuid(), nodeB, agvType, new LoadState(false), 0);
        var routingEdge = new RoutingEdge(Guid.NewGuid(), edge, agvType, routingNodeA, routingNodeB, 5.0, true, null, null, null);
        var otherRoutingNode = new RoutingNode(Guid.NewGuid(), otherNode, agvType, new LoadState(false), 0);

        await fixture.AddRoutingResourcesAsync(
            (routingNodeA, nodeA.Id),
            (routingNodeB, nodeB.Id),
            (routingEdge, edge.Id),
            (otherRoutingNode, otherNode.Id));

        var loaded = await fixture.GraphRepository.GetGraphById(graph.Id);

        loaded.ShouldNotBeNull();
        loaded.Nodes.Select(n => n.Id).ShouldBe([nodeA.Id, nodeB.Id], ignoreOrder: true);
        loaded.Edges.ShouldHaveSingleItem().Id.ShouldBe(edge.Id);
        loaded.Stations.ShouldHaveSingleItem().Id.ShouldBe(station.Id);

        var loadedNodeA = loaded.Nodes.Single(n => n.Id == nodeA.Id);
        var loadedNodeB = loaded.Nodes.Single(n => n.Id == nodeB.Id);
        var loadedEdge = loaded.Edges.Single();

        var attachedRoutingNodeA = loadedNodeA.Children.ShouldHaveSingleItem().ShouldBeOfType<RoutingNode>();
        attachedRoutingNodeA.Id.ShouldBe(routingNodeA.Id);
        attachedRoutingNodeA.Parent.ShouldBeSameAs(loadedNodeA);
        attachedRoutingNodeA.AgvType.Id.ShouldBe(agvType.Id);

        var attachedRoutingNodeB = loadedNodeB.Children.ShouldHaveSingleItem().ShouldBeOfType<RoutingNode>();
        attachedRoutingNodeB.Id.ShouldBe(routingNodeB.Id);
        attachedRoutingNodeB.Parent.ShouldBeSameAs(loadedNodeB);

        var attachedRoutingEdge = loadedEdge.Children.ShouldHaveSingleItem().ShouldBeOfType<RoutingEdge>();
        attachedRoutingEdge.Id.ShouldBe(routingEdge.Id);
        attachedRoutingEdge.Parent.ShouldBeSameAs(loadedEdge);

        attachedRoutingEdge.StartNode.ShouldBeSameAs(attachedRoutingNodeA);
        attachedRoutingEdge.EndNode.ShouldBeSameAs(attachedRoutingNodeB);
        attachedRoutingNodeA.OutgoingEdges.ShouldContain(e => e.Id == routingEdge.Id);
        attachedRoutingNodeB.IncomingEdges.ShouldContain(e => e.Id == routingEdge.Id);
    }
}
