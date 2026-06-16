using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.GraphAnalysis;

[TestClass]
public class GraphBoundingBoxCalculatorTests
{
    [TestMethod]
    public void CalculateNode_NodeWithIncidentEdge_OrientsBoxAlongEdge()
    {
        const string centerNodeName = "center";
        const string northNodeName = "north";
        var center = NodeGenerator.GenerateNode(x: 0, y: 0, theta: 0f, nodeName: centerNodeName);
        var north = NodeGenerator.GenerateNode(x: 0, y: 5, nodeName: northNodeName);
        var edge = EdgeGenerator.Generate(centerNodeName, northNodeName);
        var graph = GraphGenerator.Generate([center, north], [edge]);
        var agvData = AgvDataGenerator.GenerateAgvData(length: 4.0, width: 2.0);

        var box = new GraphBoundingBoxCalculatorTestFixture()
            .WithGraph(graph)
            .Calculator.CalculateNode(center, agvData, []);

        box.Polygon.Points.Max(p => Math.Abs(p.X)).ShouldBe(1.0, 1e-6);
        box.Polygon.Points.Max(p => Math.Abs(p.Y)).ShouldBe(2.0, 1e-6);
    }

    [TestMethod]
    public void CalculateNode_NodeAtEdgeEnd_OrientsBoxAlongEdge()
    {
        const string centerNodeName = "center";
        const string fromNodeName = "south";
        var center = NodeGenerator.GenerateNode(x: 0, y: 0, theta: 0f, nodeName: centerNodeName);
        var south = NodeGenerator.GenerateNode(x: 0, y: -5, nodeName: fromNodeName);
        var edge = EdgeGenerator.Generate(fromNodeName, centerNodeName);
        var graph = GraphGenerator.Generate([center, south], [edge]);
        var agvData = AgvDataGenerator.GenerateAgvData(length: 4.0, width: 2.0);

        var box = new GraphBoundingBoxCalculatorTestFixture()
            .WithGraph(graph)
            .Calculator.CalculateNode(center, agvData, []);

        box.Polygon.Points.Max(p => Math.Abs(p.X)).ShouldBe(1.0, 1e-6);
        box.Polygon.Points.Max(p => Math.Abs(p.Y)).ShouldBe(2.0, 1e-6);
    }

    [TestMethod]
    public void CalculateNode_IsolatedNode_FallsBackToTheta()
    {
        const string nodeName = "isolated";
        var node = NodeGenerator.GenerateNode(x: 0, y: 0, theta: (float)(Math.PI / 2), nodeName: nodeName);
        var graph = GraphGenerator.Generate([node]);
        var agvData = AgvDataGenerator.GenerateAgvData(length: 4.0, width: 2.0);

        var box = new GraphBoundingBoxCalculatorTestFixture()
            .WithGraph(graph)
            .Calculator.CalculateNode(node, agvData, []);

        box.Polygon.Points.Max(p => Math.Abs(p.X)).ShouldBe(1.0, 1e-6);
        box.Polygon.Points.Max(p => Math.Abs(p.Y)).ShouldBe(2.0, 1e-6);
    }

    [TestMethod]
    public void CalculateNode_ProviderReportsDirection_OrientsBoxAlongProviderDirection()
    {
        const string centerNodeName = "center";
        const string eastNodeName = "east";
        var center = NodeGenerator.GenerateNode(x: 0, y: 0, theta: 0f, nodeName: centerNodeName);
        var east = NodeGenerator.GenerateNode(x: 5, y: 0, nodeName: eastNodeName);
        var edge = EdgeGenerator.Generate(centerNodeName, eastNodeName);
        var graph = GraphGenerator.Generate([center, east], [edge]);
        var agvData = AgvDataGenerator.GenerateAgvData(length: 4.0, width: 2.0);

        var box = new GraphBoundingBoxCalculatorTestFixture()
            .WithGraph(graph)
            .WithProviders(new EdgePolygonProviderMock(Math.PI / 2))
            .Calculator.CalculateNode(center, agvData, []);

        box.Polygon.Points.Max(p => Math.Abs(p.X)).ShouldBe(1.0, 1e-6);
        box.Polygon.Points.Max(p => Math.Abs(p.Y)).ShouldBe(2.0, 1e-6);
    }

    private class EdgePolygonProviderMock(double direction) : IEdgePolygonProvider
    {
        public bool Supports(EdgeType edgeType)
        {
            return true;
        }

        public Polygon GetPolygon(GraphEdge edge, GraphNode from, GraphNode to, AgvData agvData, IReadOnlyList<IMarginStrategy> margins)
        {
            return new Polygon([new PolygonPoint(0, 0), new PolygonPoint(1, 0), new PolygonPoint(0, 1)]);
        }

        public double GetDirectionAtEndpoint(GraphEdge edge, GraphNode from, GraphNode to, EdgeEndpoint endpoint)
        {
            return direction;
        }
    }
}