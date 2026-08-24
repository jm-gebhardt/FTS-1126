using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;
using DomainGraph = Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph.Graph;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models.NewGraph;

[TestClass]
public class GraphTest
{
    [TestMethod]
    public void FromCommon_ValidGraph_PreservesAllResources()
    {
        var commonGraph = NewGraphGenerator.GenerateGraph();

        var graph = DomainGraph.FromCommon(commonGraph);

        graph.Nodes.Select(n => n.Id).ShouldBe(commonGraph.Nodes.Select(n => n.Id), true);
        graph.Edges.Select(e => e.Id).ShouldBe(commonGraph.Edges.Select(e => e.Id), true);
        graph.Stations.Select(s => s.Id).ShouldBe(commonGraph.Stations.Select(s => s.Id), true);
    }

    [TestMethod]
    public void FromCommon_ValidGraph_CopiesNodeProperties()
    {
        var commonGraph = NewGraphGenerator.GenerateGraph();
        var commonNode = commonGraph.Nodes.Single(n => n.Name == NewGraphGenerator.EndNodeName);

        var graph = DomainGraph.FromCommon(commonGraph);

        var node = graph.Nodes.Single(n => n.Id == commonNode.Id);
        node.Name.ShouldBe(NewGraphGenerator.EndNodeName);
        node.X.ShouldBe(commonNode.X);
        node.Y.ShouldBe(commonNode.Y);
        node.LevelId.ShouldBe(NewGraphGenerator.LevelId);
    }

    [TestMethod]
    public void FromCommon_ValidGraph_CopiesStationProperties()
    {
        var commonGraph = NewGraphGenerator.GenerateGraph();
        var commonStation = commonGraph.Stations.Single();

        var graph = DomainGraph.FromCommon(commonGraph);

        var station = graph.Stations.ShouldHaveSingleItem();
        station.Name.ShouldBe(NewGraphGenerator.StationName);
        station.StationHeight.ShouldBe(commonStation.StationHeight);
        station.X.ShouldBe(commonStation.X);
        station.Y.ShouldBe(commonStation.Y);
        station.Theta.ShouldBe(commonStation.Theta);
    }

    [TestMethod]
    public void FromCommon_ValidGraph_EdgeReferencesTheNodeIdsOfTheGraph()
    {
        var commonGraph = NewGraphGenerator.GenerateGraph();

        var graph = DomainGraph.FromCommon(commonGraph);

        var edge = graph.Edges.ShouldHaveSingleItem();
        edge.StartNodeId.ShouldBe(graph.Nodes.Single(n => n.Name == NewGraphGenerator.StartNodeName).Id);
        edge.EndNodeId.ShouldBe(graph.Nodes.Single(n => n.Name == NewGraphGenerator.EndNodeName).Id);
    }

    [TestMethod]
    public void FromCommon_ValidGraph_InteractionNodesReferenceTheNodeInstancesOfTheGraph()
    {
        var commonGraph = NewGraphGenerator.GenerateGraph();

        var graph = DomainGraph.FromCommon(commonGraph);

        var station = graph.Stations.ShouldHaveSingleItem();
        station.InteractionNodes.ShouldHaveSingleItem()
            .ShouldBeSameAs(graph.Nodes.Single(n => n.Name == NewGraphGenerator.EndNodeName));
    }

    [TestMethod]
    public void FromCommon_StationReferencesUnknownInteractionNode_Throws()
    {
        var commonGraph = NewGraphGenerator.GenerateGraph(Guid.NewGuid());

        Should.Throw<KeyNotFoundException>(() => DomainGraph.FromCommon(commonGraph));
    }
}