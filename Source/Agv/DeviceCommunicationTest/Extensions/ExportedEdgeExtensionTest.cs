using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Extensions;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Extensions;

[TestClass]
public class ExportedEdgeExtensionTest
{
    [DataRow(EdgeType.Bezier5)]
    [DataRow(EdgeType.Direct)]
    [DataTestMethod]
    public void ToGraphEdges_ValidData_CreatesEdgeOrCorrectType(EdgeType edgeType)
    {
        var startNode = new GraphNode("start");
        var endNode = new GraphNode("end");
        var exportedEdge = new ExportedEdge()
        {
            StartId = startNode.Id,
            EndId = endNode.Id,
            EdgeType = edgeType,
            Bidirectional = false,
            ControlPoints = [new ExportedControlPoint(1,2), new ExportedControlPoint(2,3), new ExportedControlPoint(3,4), new ExportedControlPoint(5,5)]
        };

        ICollection<GraphEdge> edges = exportedEdge.ToGraphEdges(startNode, endNode);
        edges.Select(e => e.Variant).Distinct().Single().ShouldBe(edgeType);
    }

    [DataRow(EdgeType.Bezier2)]
    [DataRow(EdgeType.Slanted)]
    [DataRow(EdgeType.Polypath)]
    [DataTestMethod]
    public void ToGraphEdges_NotSupportedType_ReturnsEmptyCollection(EdgeType edgeType)
    {
        var startNode = new GraphNode("start");
        var endNode = new GraphNode("end");
        var exportedEdge = new ExportedEdge()
        {
            StartId = startNode.Id,
            EndId = endNode.Id,
            EdgeType = edgeType,
            Bidirectional = false,
            ControlPoints = [new ExportedControlPoint(1, 2), new ExportedControlPoint(2, 3), new ExportedControlPoint(3, 4), new ExportedControlPoint(5, 5)]
        };

        exportedEdge.ToGraphEdges(startNode, endNode).ShouldBeEmpty();
    }

    [DataRow(true, 2)]
    [DataRow(false, 1)]
    [DataTestMethod]
    public void ToGraphEdges_Bidirectional_ReturnsCorrectNumberOfEdges(bool isBidirectional, int expectedNumberOfEdges)
    {
        var startNode = new GraphNode("start");
        var endNode = new GraphNode("end");
        var exportedEdge = new ExportedEdge()
        {
            StartId = startNode.Id,
            EndId = endNode.Id,
            EdgeType = EdgeType.Bezier5,
            Bidirectional = isBidirectional,
            ControlPoints = [new ExportedControlPoint(1, 2), new ExportedControlPoint(2, 3), new ExportedControlPoint(3, 4), new ExportedControlPoint(5, 5)]
        };
        exportedEdge.ToGraphEdges(startNode, endNode).Count.ShouldBe(expectedNumberOfEdges);
    }
}