using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Extensions;

[TestClass]
public class GraphExtensionsTest
{
    [TestMethod]
    public void GetRouteLength_ValidData_ReturnsLength()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        List<GraphEdge> neighboringEdges = graph.Edges.Take(1).ToList();
        neighboringEdges = neighboringEdges.Append(graph.Edges.First(e => e.FromNodeId == neighboringEdges.Single().ToNodeId)).ToList();
        string[] route = [neighboringEdges.First().FromNodeId, neighboringEdges.First().ToNodeId, neighboringEdges.Last().ToNodeId];

        graph.GetRouteLength(route).ShouldBe(neighboringEdges.Sum(e => e.Length));
    }

    [TestMethod]
    public void GetRouteLength_NodeInRouteDoesNotBelongToEdge_ThrowsInvalidOperationException()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();

        Should.Throw<InvalidOperationException>(() =>graph.GetRouteLength([graph.Edges.First().FromNodeId, "another node"]));
    }

    [TestMethod]
    public void GetRouteLength_OnlyOneNodeGivenInRoute_ReturnsZero()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph();
        graph.GetRouteLength([graph.Edges.First().FromNodeId]).ShouldBe(0);
    }
}