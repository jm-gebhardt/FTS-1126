using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;
using Shouldly;
using CommonGraph = Gebhardt.Acc.Common.Domain.Models.NewGraph.Graph;

namespace Gebhardt.Acc.TransportOrder.Test.TrafficManagement.Graph;

[TestClass]
public class GraphMapperTest
{
    private CommonGraph _graph = null!;
    private IReadOnlyCollection<Node> _nodes = null!;
    private IReadOnlyCollection<Edge> _edges = null!;
    private IReadOnlyCollection<Station> _stations = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _graph = NewGraphGenerator.GenerateGraph(withRoutingResources: true);

        (_nodes, _edges, _stations) = GraphMapper.Convert(_graph, GenerateAgvTypes());
    }

    private static IReadOnlyDictionary<Guid, AgvType> GenerateAgvTypes()
    {
        var agvType = AgvTypeGenerator.GenerateAgvType() with
        {
            Id = NewGraphGenerator.RoutingAgvTypeId,
            LoadStates = [new LoadState(NewGraphGenerator.RoutingLoadStateId, true)]
        };

        return new Dictionary<Guid, AgvType> { { agvType.Id, agvType } };
    }

    [TestMethod]
    public void Convert_ValidGraph_MapsAllNodesEdgesAndStations()
    {
        _nodes.Select(n => n.Id).ShouldBe(_graph.Nodes.Select(n => n.Id), true);
        _edges.Select(e => e.Id).ShouldBe(_graph.Edges.Select(e => e.Id), true);
        _stations.Select(s => s.Id).ShouldBe(_graph.Stations.Select(s => s.Id), true);
    }

    [TestMethod]
    public void Convert_ValidGraph_CopiesNodeProperties()
    {
        var commonNode = _graph.Nodes.Single(n => n.Name == NewGraphGenerator.EndNodeName);

        var node = _nodes.Single(n => n.Id == commonNode.Id);
        node.Name.ShouldBe(commonNode.Name);
        node.ShortName.ShouldBe(commonNode.ShortName);
        node.X.ShouldBe(commonNode.X);
        node.Y.ShouldBe(commonNode.Y);
        node.LevelId.ShouldBe(commonNode.LevelId);
    }

    [TestMethod]
    public void Convert_ValidGraph_EdgeReferencesMappedNodes()
    {
        var commonEdge = _graph.Edges.Single();

        var edge = _edges.ShouldHaveSingleItem();
        edge.StartNode.ShouldBeSameAs(_nodes.Single(n => n.Id == commonEdge.StartNodeId));
        edge.EndNode.ShouldBeSameAs(_nodes.Single(n => n.Id == commonEdge.EndNodeId));
    }

    [TestMethod]
    public void Convert_ValidGraph_StationReferencesMappedInteractionNode()
    {
        var commonStation = _graph.Stations.Single();

        var station = _stations.ShouldHaveSingleItem();
        station.Name.ShouldBe(commonStation.Name);
        station.StationHeight.ShouldBe(commonStation.StationHeight);
        station.X.ShouldBe(commonStation.X);
        station.Y.ShouldBe(commonStation.Y);
        station.Theta.ShouldBe(commonStation.Theta);
        station.InteractionNodes.ShouldHaveSingleItem().ShouldBeSameAs(_nodes.Single(n => n.Id == commonStation.InteractionNodeIds.Single()));
    }

    [TestMethod]
    public void Convert_WithRoutingResources_MapsRoutingNodeToStartNode()
    {
        var commonStartNode = _graph.Nodes.Single(n => n.Name == NewGraphGenerator.StartNodeName);
        var commonRoutingNode = commonStartNode.RoutingNodes.Single();

        var startNode = _nodes.Single(n => n.Id == commonStartNode.Id);
        var routingNode = startNode.RoutingNodes.ShouldHaveSingleItem();
        routingNode.Id.ShouldBe(commonRoutingNode.Id);
        routingNode.Theta.ShouldBe(commonRoutingNode.Theta);
        routingNode.AgvType.Id.ShouldBe(NewGraphGenerator.RoutingAgvTypeId);
        routingNode.LoadState.Id.ShouldBe(NewGraphGenerator.RoutingLoadStateId);
        routingNode.ParentNode.ShouldBeSameAs(startNode);
    }

    [TestMethod]
    public void Convert_WithRoutingResources_MapsRoutingEdgeFromEdge()
    {
        var commonEdge = _graph.Edges.Single();
        var commonRoutingEdge = commonEdge.RoutingEdges.Single(e => e.GetType() == typeof(Common.Domain.Models.NewGraph.RoutingEdge));

        var edge = _edges.ShouldHaveSingleItem();
        var routingEdge = edge.RoutingEdges.Single(e => e.GetType() == typeof(RoutingEdge));
        routingEdge.Id.ShouldBe(commonRoutingEdge.Id);
        routingEdge.Length.ShouldBe(commonRoutingEdge.Length);
        routingEdge.StartNode.Id.ShouldBe(commonRoutingEdge.StartNodeId);
        routingEdge.EndNode.Id.ShouldBe(commonRoutingEdge.EndNodeId);
        routingEdge.Parent.ShouldBeSameAs(edge);
    }

    [TestMethod]
    public void Convert_WithRoutingResources_MapsActionRoutingEdgeFromEdge()
    {
        var commonEdge = _graph.Edges.Single();
        var commonActionRoutingEdge = commonEdge.RoutingEdges.OfType<Common.Domain.Models.NewGraph.ActionRoutingEdge>().Single();

        var edge = _edges.ShouldHaveSingleItem();
        var actionRoutingEdge = edge.RoutingEdges.OfType<ActionRoutingEdge>().ShouldHaveSingleItem();
        actionRoutingEdge.Id.ShouldBe(commonActionRoutingEdge.Id);
        actionRoutingEdge.ActionType.ShouldBe(commonActionRoutingEdge.ActionType);
        actionRoutingEdge.Parent.ShouldBeSameAs(edge);
    }
}
