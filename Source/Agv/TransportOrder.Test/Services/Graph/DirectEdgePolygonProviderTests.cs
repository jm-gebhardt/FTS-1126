using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.Graph;

[TestClass]
public class DirectEdgePolygonProviderTests
{
    private static readonly DirectEdgePolygonProvider _provider = new();

    [TestMethod]
    [DataRow(EdgeType.Direct)]
    public void Supports_SupportedEdgeTypes_ReturnsTrue(EdgeType edgeType)
    {
        _provider.Supports(edgeType).ShouldBeTrue();
    }

    [TestMethod]
    public void GetDirectionAtEndpoint_HorizontalEdge_ReturnsZero()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 5, y: 0, nodeName: "To");
        var edge = EdgeGenerator.Generate("From", "To");

        double direction = _provider.GetDirectionAtEndpoint(edge, from, to);

        direction.ShouldBe(0.0, 1e-9);
    }

    [TestMethod]
    public void GetDirectionAtEndpoint_VerticalEdge_ReturnsHalfPi()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 0, y: 5, nodeName: "To");
        var edge = EdgeGenerator.Generate("From", "To");

        double direction = _provider.GetDirectionAtEndpoint(edge, from, to);

        direction.ShouldBe(Math.PI / 2, 1e-9);
    }

    [TestMethod]
    public void GetDirectionAtEndpoint_DiagonalEdge_ReturnsPiOverFour()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 1, y: 1, nodeName: "To");
        var edge = EdgeGenerator.Generate("From", "To");

        double direction = _provider.GetDirectionAtEndpoint(edge, from, to);

        direction.ShouldBe(Math.PI / 4, 1e-9);
    }

    [TestMethod]
    public void GetPolygon_HorizontalEdge_ReturnsRectangleWithCorrectDimensions()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 4, y: 0, nodeName: "To");
        var edge = EdgeGenerator.Generate("From", "To");
        var agvData = AgvDataGenerator.GenerateAgvData(length: 2.0, width: 1.0);

        var polygon = _provider.GetPolygon(edge, from, to, agvData, []);

        polygon.Points.Count.ShouldBeGreaterThanOrEqualTo(4);
        polygon.Points.Min(p => p.X).ShouldBe(-1.0, 1e-5);
        polygon.Points.Max(p => p.X).ShouldBe(5.0, 1e-5);
        polygon.Points.Min(p => p.Y).ShouldBe(-0.5, 1e-5);
        polygon.Points.Max(p => p.Y).ShouldBe(0.5, 1e-5);
    }

    [TestMethod]
    public void GetPolygon_WithSafetyMargin_ExpandsPolygon()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 4, y: 0, nodeName: "To");
        var edge = EdgeGenerator.Generate("From", "To");
        var agvData = AgvDataGenerator.GenerateAgvData(length: 2.0, width: 1.0);
        var margin = new SafetyMarginStrategy(0.1);

        var withMargin = _provider.GetPolygon(edge, from, to, agvData, [margin]);
        var withoutMargin = _provider.GetPolygon(edge, from, to, agvData, []);

        withMargin.Points.Max(p => p.Y).ShouldBeGreaterThan(withoutMargin.Points.Max(p => p.Y));
        withMargin.Points.Min(p => p.Y).ShouldBeLessThan(withoutMargin.Points.Min(p => p.Y));
    }

    [TestMethod]
    public void GetPolygon_VerticalEdge_SwapsDimensions()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 0, y: 4, nodeName: "To");
        var edge = EdgeGenerator.Generate("From", "To");
        var agvData = AgvDataGenerator.GenerateAgvData(length: 2.0, width: 1.0);

        var polygon = _provider.GetPolygon(edge, from, to, agvData, []);

        // For a vertical (north-going) edge, length is in Y direction, width is in X direction
        polygon.Points.Max(p => p.X).ShouldBe(0.5, 1e-5);
        polygon.Points.Min(p => p.X).ShouldBe(-0.5, 1e-5);
        polygon.Points.Min(p => p.Y).ShouldBe(-1.0, 1e-5);
        polygon.Points.Max(p => p.Y).ShouldBe(5.0, 1e-5);
    }

    [TestMethod]
    public void GetPolygon_ZeroLengthEdge_ReturnsPolygonWithFiniteCoordinates()
    {
        var from = NodeGenerator.GenerateNode(x: 3, y: 3, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 3, y: 3, nodeName: "To");
        var edge = EdgeGenerator.Generate("From", "To");
        var agvData = AgvDataGenerator.GenerateAgvData(length: 2.0, width: 1.0);

        var polygon = _provider.GetPolygon(edge, from, to, agvData, []);

        polygon.Points.ShouldAllBe(p => !double.IsNaN(p.X) && !double.IsNaN(p.Y)
                                     && !double.IsInfinity(p.X) && !double.IsInfinity(p.Y));
    }
}
