using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

[TestClass]
public class GraphProfileTest
{
    private static Mapper GetMapper()
    {
        return MappingHelper.GetMapper(new GraphProfile());
    }

    private static void VerifyAgvGraph(EfGraph efGraph, AgvGraph agvGraph)
    {
        efGraph.ShouldNotBeNull();
        efGraph.ShouldSatisfyAllConditions(
            e => e.CorrelationId.ShouldBe(agvGraph.CorrelationId),
            e => e.RelatedMapId.ShouldBe(agvGraph.RelatedMapId),
            e => e.Usability.ToString().ShouldBe(agvGraph.Usability.ToString()));
        efGraph.Nodes.Count.ShouldBe(agvGraph.Nodes.Count);
        efGraph.Edges.Count.ShouldBe(agvGraph.Edges.Count);
    }

    private static void VerifyGraphEdge(EfEdge efEdge, GraphEdge graphEdge)
    {
        efEdge.ShouldNotBeNull();
        efEdge.ShouldSatisfyAllConditions(
            e => e.Id.ShouldBe(graphEdge.Id),
            e => e.FromNodeId.ShouldBe(graphEdge.FromNodeId),
            e => e.ToNodeId.ShouldBe(graphEdge.ToNodeId),
            e => e.Length.ShouldBe(graphEdge.Length));
    }

    private static void VerifyGraphNode(EfNode efNode, GraphNode graphNode)
    {
        efNode.ShouldNotBeNull();
        efNode.ShouldSatisfyAllConditions(
            n => n.Id.ShouldBe(graphNode.Id),
            n => n.Type.ToString().ShouldBe(graphNode.Type.ToString()),
            n => n.X.ShouldBe(graphNode.X),
            n => n.Y.ShouldBe(graphNode.Y),
            n => n.Z.ShouldBe(graphNode.Z),
            n => n.Theta.ShouldBe(graphNode.Theta));
    }

    private static void VerifyGraphEdges(EfGraph efGraph, AgvGraph agvGraph)
    {
        for (int i = 0; i < efGraph.Edges.Count; i++)
        {
            VerifyGraphEdge(efGraph.Edges.ElementAt(i), agvGraph.Edges.ElementAt(i));
        }
    }

    private static void VerifyGraphNodes(EfGraph efGraph, AgvGraph agvGraph)
    {
        for (int i = 0; i < efGraph.Nodes.Count; i++)
        {
            VerifyGraphNode(efGraph.Nodes.ElementAt(i), agvGraph.Nodes.ElementAt(i));
        }
    }

    [TestMethod]
    public void Map_WhereAgvGraphIsMappedToEfGraph_ShouldMapAllPropertiesCorrectly()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(agvCorrelationId);
        var mapper = GetMapper();

        var efGraph = mapper.Map<EfGraph>(agvGraph);

        VerifyAgvGraph(efGraph, agvGraph);
        efGraph.CorrelationId.ShouldBe(agvCorrelationId);
        VerifyGraphNodes(efGraph, agvGraph);
        VerifyGraphEdges(efGraph, agvGraph);
    }

    [TestMethod]
    public void Map_WhereGraphEdgeIsMappedToEfEdge_ShouldMapAllPropertiesCorrectly()
    {
        var graphEdge = AgvGraphGenerator.Edges.First();
        var mapper = GetMapper();

        var efEdge = mapper.Map<EfEdge>(graphEdge);

        VerifyGraphEdge(efEdge, graphEdge);
    }

    [TestMethod]
    public void Map_WhereGraphNodeIsMappedToEfNode_ShouldMapAllPropertiesCorrectly()
    {
        var graphNode = AgvGraphGenerator.Nodes.First();
        var mapper = GetMapper();

        var efNode = mapper.Map<EfNode>(graphNode);

        VerifyGraphNode(efNode, graphNode);
    }
}