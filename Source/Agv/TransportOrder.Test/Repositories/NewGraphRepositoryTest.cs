using Gebhardt.Acc.Common.Domain.Models.NewGraph;
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
        var graphSegment = NewGraphGenerator.GenerateSegmentGraph();
        var commonEdge = graphSegment.Edges.Single();

        await fixture.GraphRepository.AddOrUpdateGraphSegmentAsync(graphSegment);

        var loaded = await fixture.GraphRepository.GetGraphAsync();
        loaded.Nodes.Select(n => n.Id).ShouldBe(graphSegment.Nodes.Select(n => n.Id), true);

        var loadedEdge = loaded.Edges.ShouldHaveSingleItem();
        loadedEdge.Id.ShouldBe(commonEdge.Id);
        loadedEdge.StartNodeId.ShouldBe(commonEdge.StartNodeId);
        loadedEdge.EndNodeId.ShouldBe(commonEdge.EndNodeId);

        var loadedStation = loaded.Stations.ShouldHaveSingleItem();
        loadedStation.Id.ShouldBe(graphSegment.Stations.Single().Id);
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

        var nodeA = new Node(nodeAId, Fixture.NodeAName, Fixture.NodeAName, Fixture.NodeAName, 0, 0, null, null);
        var nodeB = new Node(nodeBId, Fixture.NodeBName, Fixture.NodeBName, Fixture.NodeBName, 1, 1, null, null);
        await fixture.GraphRepository.AddOrUpdateGraphSegmentAsync(Fixture.CreateGraph(
            [nodeA, nodeB],
            [new Edge(edgeAbId, Fixture.EdgeAbName, Fixture.EdgeAbName, Fixture.EdgeAbName, nodeA.Id, nodeB.Id)],
            [Fixture.CreateStation(stationId, nodeA)]));

        var movedNodeA = new Node(nodeAId, Fixture.NodeAName, Fixture.NodeAName, Fixture.NodeAName, Fixture.MovedNodePosition, Fixture.MovedNodePosition, null, null);
        var reuploadedNodeB = new Node(nodeBId, Fixture.NodeBName, Fixture.NodeBName, Fixture.NodeBName, 1, 1, null, null);
        var nodeC = new Node(nodeCId, Fixture.NodeCName, Fixture.NodeCName, Fixture.NodeCName, 2, 2, null, null);
        await fixture.GraphRepository.AddOrUpdateGraphSegmentAsync(Fixture.CreateGraph(
            [movedNodeA, reuploadedNodeB, nodeC],
            [new Edge(edgeAbId, Fixture.EdgeAbName, Fixture.EdgeAbName, Fixture.EdgeAbName, movedNodeA.Id, reuploadedNodeB.Id), new Edge(edgeAcId, Fixture.EdgeAcName, Fixture.EdgeAcName, Fixture.EdgeAcName, movedNodeA.Id, nodeC.Id)],
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
        var node = new Node(nodeId, Fixture.NodeAName, Fixture.NodeAName, Fixture.NodeAName, 0, 0, null, null);
        await fixture.GraphRepository.AddOrUpdateGraphSegmentAsync(Fixture.CreateGraph([node]));

        var agvType = AgvTypeGenerator.GenerateAgvType(Fixture.AgvTypeName);
        var routingNode = new RoutingNode(Guid.NewGuid(), agvType.Id, Guid.NewGuid(), 0);
        await fixture.AddRoutingResourcesAsync((routingNode, node));

        await fixture.GraphRepository.AddOrUpdateGraphSegmentAsync(Fixture.CreateGraph(
            [new Node(nodeId, Fixture.NodeAName, Fixture.NodeAName, Fixture.NodeAName, Fixture.MovedNodePosition, Fixture.MovedNodePosition, null, null)]));

        var loaded = await fixture.GraphRepository.GetGraphAsync();
        var loadedNode = loaded.Nodes.ShouldHaveSingleItem();
        loadedNode.X.ShouldBe(Fixture.MovedNodePosition);
        loadedNode.RoutingNodes.ShouldHaveSingleItem().Id.ShouldBe(routingNode.Id);
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

        var nodeA = new Node(Guid.NewGuid(), Fixture.NodeAName, Fixture.NodeAName, Fixture.NodeAName, 0, 0, null, null);
        var nodeB = new Node(Guid.NewGuid(), Fixture.NodeBName, Fixture.NodeBName, Fixture.NodeBName, 1, 1, null, null);
        var edge = new Edge(Guid.NewGuid(), Fixture.EdgeAbName, Fixture.EdgeAbName, Fixture.EdgeAbName, nodeA.Id, nodeB.Id);
        var station = new Station(Guid.NewGuid(), Fixture.StationName, Fixture.StationName, Fixture.StationName, 0, 0, null, null, []);
        await fixture.AddResourcesAsync([nodeA, nodeB], [edge], [station]);

        var agvType = AgvTypeGenerator.GenerateAgvType(Fixture.AgvTypeName);
        var routingNodeA = new RoutingNode(Guid.NewGuid(), agvType.Id, Guid.NewGuid(), 0);
        var routingNodeB = new RoutingNode(Guid.NewGuid(), agvType.Id, Guid.NewGuid(), 0);
        var routingEdge = new RoutingEdge(Guid.NewGuid(), agvType.Id, routingNodeA.Id, routingNodeB.Id, 5.0, Math.PI, OrientationType.Tangential, null, null,
            false, null, null, null);

        await fixture.AddRoutingResourcesAsync(
            (routingNodeA, nodeA),
            (routingNodeB, nodeB),
            (routingEdge, edge));

        var loaded = await fixture.GraphRepository.GetGraphAsync();

        loaded.Nodes.Select(n => n.Id).ShouldBe([nodeA.Id, nodeB.Id], true);
        loaded.Edges.ShouldHaveSingleItem().Id.ShouldBe(edge.Id);
        loaded.Stations.ShouldHaveSingleItem().Id.ShouldBe(station.Id);

        var loadedNodeA = loaded.Nodes.Single(n => n.Id == nodeA.Id);
        var loadedNodeB = loaded.Nodes.Single(n => n.Id == nodeB.Id);
        var loadedEdge = loaded.Edges.Single();

        var attachedRoutingNodeA = loadedNodeA.RoutingNodes.ShouldHaveSingleItem().ShouldBeOfType<RoutingNode>();
        attachedRoutingNodeA.Id.ShouldBe(routingNodeA.Id);
        attachedRoutingNodeA.AgvTypeId.ShouldBe(agvType.Id);

        var attachedRoutingNodeB = loadedNodeB.RoutingNodes.ShouldHaveSingleItem().ShouldBeOfType<RoutingNode>();
        attachedRoutingNodeB.Id.ShouldBe(routingNodeB.Id);

        var attachedRoutingEdge = loadedEdge.RoutingEdges.ShouldHaveSingleItem().ShouldBeOfType<RoutingEdge>();
        attachedRoutingEdge.Id.ShouldBe(routingEdge.Id);

        attachedRoutingEdge.StartNodeId.ShouldBe(attachedRoutingNodeA.Id);
        attachedRoutingEdge.EndNodeId.ShouldBe(attachedRoutingNodeB.Id);
    }
}