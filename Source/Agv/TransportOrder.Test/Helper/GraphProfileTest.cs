using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

[TestClass]
public class GraphProfileTest
{
    private static Mapper GetMapper()
    {
        return MappingHelper.GetMapper(new GraphProfile());
    }

    private static void VerifyAgvGraph(AgvGraph agvGraph1, AgvGraph agvGraph2)
    {
        agvGraph1.ShouldNotBeNull();
        agvGraph1.ShouldSatisfyAllConditions(
            e => e.CorrelationId.ShouldBe(agvGraph2.CorrelationId),
            e => e.Usability.ToString().ShouldBe(agvGraph2.Usability.ToString()));
        agvGraph1.Nodes.Count.ShouldBe(agvGraph2.Nodes.Count);
        agvGraph1.Edges.Count.ShouldBe(agvGraph2.Edges.Count);
    }

    private static void VerifyGraphEdge(GraphEdge efEdge, GraphEdge graphEdge)
    {
        efEdge.ShouldNotBeNull();
        efEdge.ShouldSatisfyAllConditions(
            e => e.Id.ShouldBe(graphEdge.Id),
            e => e.FromNodeId.ShouldBe(graphEdge.FromNodeId),
            e => e.ToNodeId.ShouldBe(graphEdge.ToNodeId),
            e => e.Length.ShouldBe(graphEdge.Length));
    }

    private static void VerifyGraphNode(GraphNode efNode, GraphNode graphNode)
    {
        efNode.ShouldNotBeNull();
        efNode.ShouldSatisfyAllConditions(
            n => n.Name.ShouldBe(graphNode.Name),
            n => n.Type.ToString().ShouldBe(graphNode.Type.ToString()),
            n => n.X.ShouldBe(graphNode.X),
            n => n.Y.ShouldBe(graphNode.Y),
            n => n.Z.ShouldBe(graphNode.Z),
            n => n.Theta.ShouldBe(graphNode.Theta));
    }

    private static void VerifyGraphEdges(AgvGraph agvGraph1, AgvGraph agvGraph2)
    {
        for (int i = 0; i < agvGraph1.Edges.Count; i++)
        {
            VerifyGraphEdge(agvGraph1.Edges.ElementAt(i), agvGraph2.Edges.ElementAt(i));
        }
    }

    private static void VerifyGraphNodes(AgvGraph agvGraph1, AgvGraph agvGraph2)
    {
        for (int i = 0; i < agvGraph1.Nodes.Count; i++)
        {
            VerifyGraphNode(agvGraph1.Nodes.ElementAt(i), agvGraph2.Nodes.ElementAt(i));
        }
    }

    [TestMethod]
    public void Map_WhereAgvGraphIsMappedToEfGraph_ShouldMapAllPropertiesCorrectly()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(agvCorrelationId);
        var mapper = GetMapper();

        var efGraph = mapper.Map<AgvGraph>(agvGraph);

        VerifyAgvGraph(efGraph, agvGraph);
        efGraph.CorrelationId.ShouldBe(agvCorrelationId);
        VerifyGraphNodes(efGraph, agvGraph);
        VerifyGraphEdges(efGraph, agvGraph);
    }

    [TestMethod]
    public void Map_WhereGraphEdgeIsMappedToGraphEdge_ShouldMapAllPropertiesCorrectly()
    {
        var graphEdge = AgvGraphGenerator.Edges.First();
        var mapper = GetMapper();

        var efEdge = mapper.Map<GraphEdge>(graphEdge);

        VerifyGraphEdge(efEdge, graphEdge);
    }

    [TestMethod]
    public void Map_WhereGraphNodeIsMappedToGraphNode_ShouldMapAllPropertiesCorrectly()
    {
        var graphNode = AgvGraphGenerator.Nodes.First();
        var mapper = GetMapper();

        var efNode = mapper.Map<GraphNode>(graphNode);

        VerifyGraphNode(efNode, graphNode);
    }
}