using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;
using AgvTypeGenerator = Gebhardt.Acc.Common.Testing.Helpers.AgvTypeGenerator;
using Fixture = Gebhardt.Acc.TransportOrder.Test.Services.Graph.GraphBoundingBoxCalculatorTestFixture;

namespace Gebhardt.Acc.TransportOrder.Test.Services.Graph;

[TestClass]
public class GraphBoundingBoxCalculatorTests
{
    [TestMethod]
    public void CalculateNode_NodeWithIncidentEdge_OrientsBoxAlongEdge()
    {
        var center = RoutingGraphGenerator.GenerateNode(0, 0);
        var north = RoutingGraphGenerator.GenerateNode(0, 5);
        var edge = RoutingGraphGenerator.GenerateEdge(center, north);
        var graph = RoutingGraphGenerator.GenerateGraph([center, north], [edge]);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);

        var box = new Fixture()
            .WithGraph(graph)
            .Calculator.CalculateNode(RoutingGraphGenerator.GetRoutingNode(center), agvType, []);

        box.Polygon.Points.Max(p => Math.Abs(p.X)).ShouldBe(1.0, 1e-6);
        box.Polygon.Points.Max(p => Math.Abs(p.Y)).ShouldBe(2.0, 1e-6);
    }

    [TestMethod]
    public void CalculateNode_NodeAtEdgeEnd_OrientsBoxAlongEdge()
    {
        var center = RoutingGraphGenerator.GenerateNode(0, 0);
        var south = RoutingGraphGenerator.GenerateNode(0, -5);
        var edge = RoutingGraphGenerator.GenerateEdge(south, center);
        var graph = RoutingGraphGenerator.GenerateGraph([center, south], [edge]);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);

        var box = new Fixture()
            .WithGraph(graph)
            .Calculator.CalculateNode(RoutingGraphGenerator.GetRoutingNode(center), agvType, []);

        box.Polygon.Points.Max(p => Math.Abs(p.X)).ShouldBe(1.0, 1e-6);
        box.Polygon.Points.Max(p => Math.Abs(p.Y)).ShouldBe(2.0, 1e-6);
    }

    [TestMethod]
    public void CalculateNode_IsolatedNode_FallsBackToTheta()
    {
        var node = RoutingGraphGenerator.GenerateNode(0, 0, Math.PI / 2);
        var graph = RoutingGraphGenerator.GenerateGraph([node]);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);

        var box = new Fixture()
            .WithGraph(graph)
            .Calculator.CalculateNode(RoutingGraphGenerator.GetRoutingNode(node), agvType, []);

        box.Polygon.Points.Max(p => Math.Abs(p.X)).ShouldBe(1.0, 1e-6);
        box.Polygon.Points.Max(p => Math.Abs(p.Y)).ShouldBe(2.0, 1e-6);
    }

    [TestMethod]
    public void CalculateNode_ProviderReportsDirection_OrientsBoxAlongProviderDirection()
    {
        var center = RoutingGraphGenerator.GenerateNode(0, 0);
        var east = RoutingGraphGenerator.GenerateNode(5, 0);
        var edge = RoutingGraphGenerator.GenerateEdge(center, east);
        var graph = RoutingGraphGenerator.GenerateGraph([center, east], [edge]);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);

        var box = new Fixture()
            .WithGraph(graph)
            .WithProviderDirection(Math.PI / 2)
            .Calculator.CalculateNode(RoutingGraphGenerator.GetRoutingNode(center), agvType, []);

        box.Polygon.Points.Max(p => Math.Abs(p.X)).ShouldBe(1.0, 1e-6);
        box.Polygon.Points.Max(p => Math.Abs(p.Y)).ShouldBe(2.0, 1e-6);
    }

    [TestMethod]
    public void CalculateAll_GraphWithNodeAndEdge_ReturnsBoxPerRoutingResource()
    {
        var from = RoutingGraphGenerator.GenerateNode(0, 0);
        var to = RoutingGraphGenerator.GenerateNode(5, 0);
        var edge = RoutingGraphGenerator.GenerateEdge(from, to);
        var graph = RoutingGraphGenerator.GenerateGraph([from, to], [edge]);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(RoutingGraphGenerator.AgvTypeIdentifier,
            length: Fixture.AgvLength, width: Fixture.AgvWidth);

        IReadOnlyList<ResourceBoundingBox> boxes = new Fixture()
            .WithGraph(graph)
            .Calculator.CalculateAll(agvType, []);

        boxes.Count(b => b.ResourceType == ResourceType.Node).ShouldBe(2);
        boxes.Count(b => b.ResourceType == ResourceType.Edge).ShouldBe(1);
    }
}