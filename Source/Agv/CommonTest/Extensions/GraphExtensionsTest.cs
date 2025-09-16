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