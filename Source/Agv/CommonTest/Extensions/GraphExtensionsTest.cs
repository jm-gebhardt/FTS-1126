using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Extensions;

[TestClass]
public class GraphExtensionsTest
{
    [TestMethod]
    public void GetRouteLength_ValidData_ReturnsLength()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        List<GraphEdge> neighboringEdges = graph.Edges.Take(1).ToList();
        neighboringEdges = neighboringEdges.Append(graph.Edges.First(e => e.FromNodeId == neighboringEdges.Single().ToNodeId)).ToList();
        GraphNode[] route =
        [
            neighboringEdges.First().FromNodeId.ToGraphNode(),
            neighboringEdges.First().ToNodeId.ToGraphNode(),
            neighboringEdges.Last().ToNodeId.ToGraphNode()
        ];

        graph.GetRouteLength(route).ShouldBe(neighboringEdges.Sum(e => e.Length));
    }

    [TestMethod]
    public void GetRouteLength_FromEdgeIds_NoLastNodeId_ReturnsLength()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        List<GraphEdge> neighboringEdges = graph.GetNeighboringEdges(3).ToList();
        graph.GetRouteLength(neighboringEdges.Select(e => e.Id).ToArray(), null).ShouldBe(neighboringEdges.Sum(e => e.Length));
    }

    [TestMethod]
    public void GetRouteLength_FromEdgeIds_AgvAtEndOfRoute_ReturnsLength()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        List<GraphEdge> neighboringEdges = graph.GetNeighboringEdges(3).ToList();
        var lastNodeId = graph.Edges.Single(e => e.Id == neighboringEdges.Last().Id).ToNodeId;
        graph.GetRouteLength(neighboringEdges.Select(e => e.Id).ToArray(), lastNodeId).ShouldBe(neighboringEdges.Sum(e => e.Length));
    }

    [TestMethod]
    public void GetRouteLength_FromEdgeIds_AgvInMiddleOfRoute_ReturnsLength()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        List<GraphEdge> neighboringEdges = graph.GetNeighboringEdges(3).ToList();
        var lastNodeId = graph.Edges.Single(e => e.Id == neighboringEdges.First().Id).ToNodeId;
        graph.GetRouteLength(neighboringEdges.Select(e => e.Id).ToArray(), lastNodeId).ShouldBe(neighboringEdges.First().Length);
    }

    [TestMethod]
    public void GetRouteLength_NodeInRouteDoesNotBelongToEdge_ThrowsInvalidOperationException()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();

        Should.Throw<InvalidOperationException>(() => graph.GetRouteLength([graph.Edges.First().FromNodeId.ToGraphNode(), "another node".ToGraphNode()]));
    }

    [TestMethod]
    public void GetRouteLength_OnlyOneNodeGivenInRoute_ReturnsZero()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        graph.GetRouteLength([graph.Edges.First().FromNodeId.ToGraphNode()]).ShouldBe(0);
    }
}