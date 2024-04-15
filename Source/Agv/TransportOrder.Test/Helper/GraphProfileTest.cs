using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Helper;

[TestClass]
public class GraphProfileTest
{
    private static Mapper GetMapper()
    {
        return MappingHelper.GetMapper(new GraphProfile());
    }

    private static void VerifyAgvGraph(EfAgvGraph efAgvGraph, AgvGraph agvGraph)
    {
        efAgvGraph.ShouldNotBeNull();
        efAgvGraph.ShouldSatisfyAllConditions(
            e => e.CorrelationId.ShouldBe(agvGraph.CorrelationId),
            e => e.Usability.ToString().ShouldBe(agvGraph.Usability.ToString()));
        efAgvGraph.Nodes.Count.ShouldBe(agvGraph.Nodes.Count);
        efAgvGraph.Edges.Count.ShouldBe(agvGraph.Edges.Count);
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

    private static void VerifyGraphEdges(EfAgvGraph efAgvGraph, AgvGraph agvGraph)
    {
        for (int i = 0; i < efAgvGraph.Edges.Count; i++)
        {
            VerifyGraphEdge(efAgvGraph.Edges.ElementAt(i), agvGraph.Edges.ElementAt(i));
        }
    }

    private static void VerifyGraphNodes(EfAgvGraph efAgvGraph, AgvGraph agvGraph)
    {
        for (int i = 0; i < efAgvGraph.Nodes.Count; i++)
        {
            VerifyGraphNode(efAgvGraph.Nodes.ElementAt(i), agvGraph.Nodes.ElementAt(i));
        }
    }

    [TestMethod]
    public void Map_WhereAgvGraphIsMappedToEfGraph_ShouldMapAllPropertiesCorrectly()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(agvCorrelationId);
        var mapper = GetMapper();

        var efGraph = mapper.Map<EfAgvGraph>(agvGraph);

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