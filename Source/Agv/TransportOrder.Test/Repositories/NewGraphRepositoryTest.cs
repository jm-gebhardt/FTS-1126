using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;
using Fixture = Gebhardt.Acc.TransportOrder.Test.Repositories.NewGraphRepositoryTestFixture;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class NewGraphRepositoryTest
{
    [TestMethod]
    public async Task AddOrUpdateGraphAsync_GraphConvertedFromCommon_RoundTripsNodesEdgesStationsAndInteractionNodes()
    {
        var fixture = new Fixture();
        var commonGraph = NewGraphGenerator.GenerateGraph();
        var commonEdge = commonGraph.Edges.Single();

        await fixture.GraphRepository.AddOrUpdateGraphAsync(Graph.FromCommon(commonGraph));

        var loaded = await fixture.GraphRepository.GetGraphAsync();
        loaded.Nodes.Select(n => n.Id).ShouldBe(commonGraph.Nodes.Select(n => n.Id), true);

        var loadedEdge = loaded.Edges.ShouldHaveSingleItem();
        loadedEdge.Id.ShouldBe(commonEdge.Id);
        loadedEdge.StartNodeId.ShouldBe(commonEdge.StartNodeId);
        loadedEdge.EndNodeId.ShouldBe(commonEdge.EndNodeId);

        var loadedStation = loaded.Stations.ShouldHaveSingleItem();
        loadedStation.Id.ShouldBe(commonGraph.Stations.Single().Id);
        loadedStation.InteractionNodes.ShouldHaveSingleItem()
            .ShouldBeSameAs(loaded.Nodes.Single(n => n.Id == commonEdge.EndNodeId));
    }

    [TestMethod]
    public async Task AddOrUpdateGraphAsync_GraphUploadedAgain_UpdatesKnownResourcesInPlaceAndAddsTheNewOnes()
    {
        var fixture = new Fixture();
        var nodeAId = Guid.NewGuid();
        var nodeBId = Guid.NewGuid();
        var nodeCId = Guid.NewGuid();
        var edgeAbId = Guid.NewGuid();
        var edgeAcId = Guid.NewGuid();
        var stationId = Guid.NewGuid();

        var nodeA = new Node(nodeAId, Fixture.NodeAName, null, 0, 0, null);
        var nodeB = new Node(nodeBId, Fixture.NodeBName, null, 1, 1, null);
        await fixture.GraphRepository.AddOrUpdateGraphAsync(Fixture.CreateGraph(
            [nodeA, nodeB],
            [new Edge(edgeAbId, Fixture.EdgeAbName, null, nodeA, nodeB)],
            [Fixture.CreateStation(stationId, nodeA)]));

        var movedNodeA = new Node(nodeAId, Fixture.NodeAName, null, Fixture.MovedNodePosition, Fixture.MovedNodePosition, null);
        var reuploadedNodeB = new Node(nodeBId, Fixture.NodeBName, null, 1, 1, null);
        var nodeC = new Node(nodeCId, Fixture.NodeCName, null, 2, 2, null);
        await fixture.GraphRepository.AddOrUpdateGraphAsync(Fixture.CreateGraph(
            [movedNodeA, reuploadedNodeB, nodeC],
            [new Edge(edgeAbId, Fixture.EdgeAbName, null, movedNodeA, reuploadedNodeB), new Edge(edgeAcId, Fixture.EdgeAcName, null, movedNodeA, nodeC)],
            [Fixture.CreateStation(stationId, nodeC)]));

        var loaded = await fixture.GraphRepository.GetGraphAsync();
        loaded.Nodes.Select(n => n.Id).ShouldBe([nodeAId, nodeBId, nodeCId], true);
        loaded.Nodes.Single(n => n.Id == nodeAId).X.ShouldBe(Fixture.MovedNodePosition);
        loaded.Edges.Select(e => e.Id).ShouldBe([edgeAbId, edgeAcId], true);
        loaded.Stations.ShouldHaveSingleItem().InteractionNodes.ShouldHaveSingleItem().Id.ShouldBe(nodeCId);
    }

    [TestMethod]
    public async Task AddOrUpdateGraphAsync_KnownNodeUploadedAgain_KeepsItsRoutingResourceChildren()
    {
        var fixture = new Fixture();
        var nodeId = Guid.NewGuid();
        var node = new Node(nodeId, Fixture.NodeAName, null, 0, 0, null);
        await fixture.GraphRepository.AddOrUpdateGraphAsync(Fixture.CreateGraph([node]));

        var agvType = new AgvType(Guid.NewGuid(), Fixture.AgvTypeName);
        var routingNode = new RoutingNode(Guid.NewGuid(), node, agvType, new LoadState(false), 0);
        await fixture.AddRoutingResourcesAsync((routingNode, nodeId));

        await fixture.GraphRepository.AddOrUpdateGraphAsync(Fixture.CreateGraph(
            [new Node(nodeId, Fixture.NodeAName, null, Fixture.MovedNodePosition, Fixture.MovedNodePosition, null)]));

        var loaded = await fixture.GraphRepository.GetGraphAsync();
        var loadedNode = loaded.Nodes.ShouldHaveSingleItem();
        loadedNode.X.ShouldBe(Fixture.MovedNodePosition);
        loadedNode.Children.ShouldHaveSingleItem().Id.ShouldBe(routingNode.Id);
    }

    [TestMethod]
    public async Task GetGraphAsync_NothingStored_ReturnsAnEmptyGraph()
    {
        var fixture = new Fixture();

        var loaded = await fixture.GraphRepository.GetGraphAsync();

        loaded.Nodes.ShouldBeEmpty();
        loaded.Edges.ShouldBeEmpty();
        loaded.Stations.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetGraphAsync_ResourcesStored_LoadsNodesEdgesAndStationsWithTheirRoutingResourceChildren()
    {
        var fixture = new Fixture();

        var nodeA = new Node(Guid.NewGuid(), Fixture.NodeAName, null, 0, 0, null);
        var nodeB = new Node(Guid.NewGuid(), Fixture.NodeBName, null, 1, 1, null);
        var edge = new Edge(Guid.NewGuid(), Fixture.EdgeAbName, null, nodeA, nodeB);
        var station = new Station(Guid.NewGuid(), Fixture.StationName, null, null, 0, 0, null);
        await fixture.AddResourcesAsync([nodeA, nodeB], [edge], [station]);

        var agvType = new AgvType(Guid.NewGuid(), Fixture.AgvTypeName);
        var routingNodeA = new RoutingNode(Guid.NewGuid(), nodeA, agvType, new LoadState(false), 0);
        var routingNodeB = new RoutingNode(Guid.NewGuid(), nodeB, agvType, new LoadState(false), 0);
        var routingEdge = new RoutingEdge(Guid.NewGuid(), edge, agvType, routingNodeA, routingNodeB, 5.0, true, null, null, null);

        await fixture.AddRoutingResourcesAsync(
            (routingNodeA, nodeA.Id),
            (routingNodeB, nodeB.Id),
            (routingEdge, edge.Id));

        var loaded = await fixture.GraphRepository.GetGraphAsync();

        loaded.Nodes.Select(n => n.Id).ShouldBe([nodeA.Id, nodeB.Id], true);
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