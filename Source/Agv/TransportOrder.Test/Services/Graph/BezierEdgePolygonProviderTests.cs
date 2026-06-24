using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.Graph;

[TestClass]
public class BezierEdgePolygonProviderTests
{
    private static readonly BezierEdgePolygonProvider _provider = new();

    [TestMethod]
    [DataRow(EdgeType.Bezier2)]
    [DataRow(EdgeType.Bezier3)]
    [DataRow(EdgeType.Bezier5)]
    public void Supports_SupportedEdgeTypes_ReturnsTrue(EdgeType edgeType)
    {
        _provider.Supports(edgeType).ShouldBeTrue();
    }

    [TestMethod]
    public void GetDirectionAtEndpoint_StraightHorizontalEdge_ReturnsZero()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 4, y: 0, nodeName: "To");
        // Full Bezier3 control polygon including start and end: (0,0)→(1,0)→(3,0)→(4,0), all on x-axis.
        var edge = EdgeGenerator.Generate("From", "To", length: 4.0, variant: EdgeType.Bezier3,
            variantParameters: [new EdgeCoefficient(0, 0), new EdgeCoefficient(1, 0), new EdgeCoefficient(3, 0), new EdgeCoefficient(4, 0)]);

        double direction = _provider.GetDirectionAtEndpoint(edge, from, to);

        direction.ShouldBe(0.0, 1e-5);
    }

    [TestMethod]
    public void GetDirectionAtEndpoint_StraightVerticalEdge_ReturnsHalfPi()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 0, y: 4, nodeName: "To");
        var edge = EdgeGenerator.Generate("From", "To", length: 4.0, variant: EdgeType.Bezier3,
            variantParameters: [new EdgeCoefficient(0, 0), new EdgeCoefficient(0, 1), new EdgeCoefficient(0, 3), new EdgeCoefficient(0, 4)]);

        double direction = _provider.GetDirectionAtEndpoint(edge, from, to);

        direction.ShouldBe(Math.PI / 2, 1e-5);
    }

    [TestMethod]
    public void GetDirectionAtEndpoint_StraightDiagonalEdge_ReturnsPiOverFour()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 3, y: 3, nodeName: "To");
        var edge = EdgeGenerator.Generate("From", "To", length: 3 * Math.Sqrt(2), variant: EdgeType.Bezier3,
            variantParameters: [new EdgeCoefficient(0, 0), new EdgeCoefficient(1, 1), new EdgeCoefficient(2, 2), new EdgeCoefficient(3, 3)]);

        double direction = _provider.GetDirectionAtEndpoint(edge, from, to);

        direction.ShouldBe(Math.PI / 4, 1e-4);
    }

    [TestMethod]
    public void GetPolygon_StraightHorizontalBezier3_ReturnsCorrectBoundingRectangle()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 4, y: 0, nodeName: "To");
        var edge = EdgeGenerator.Generate("From", "To", length: 4.0, variant: EdgeType.Bezier3,
            variantParameters: [new EdgeCoefficient(0, 0), new EdgeCoefficient(1, 0), new EdgeCoefficient(3, 0), new EdgeCoefficient(4, 0)]);
        var agvData = AgvDataGenerator.GenerateAgvData(length: 2.0, width: 1.0);

        var polygon = _provider.GetPolygon(edge, from, to, agvData, []);

        polygon.Points.Min(p => p.X).ShouldBe(-1.0, 1e-5);
        polygon.Points.Max(p => p.X).ShouldBe(5.0, 1e-5);
        polygon.Points.Min(p => p.Y).ShouldBe(-0.5, 1e-5);
        polygon.Points.Max(p => p.Y).ShouldBe(0.5, 1e-5);
    }

    [TestMethod]
    public void GetPolygon_StraightBezier3_CollinearPointsRemoved_PolygonHasFourCorners()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 4, y: 0, nodeName: "To");
        var edge = EdgeGenerator.Generate("From", "To", length: 4.0, variant: EdgeType.Bezier3,
            variantParameters: [new EdgeCoefficient(0, 0), new EdgeCoefficient(1, 0), new EdgeCoefficient(3, 0), new EdgeCoefficient(4, 0)]);
        var agvData = AgvDataGenerator.GenerateAgvData(length: 2.0, width: 1.0);

        var polygon = _provider.GetPolygon(edge, from, to, agvData, []);

        polygon.Points.Count.ShouldBe(4);
    }

    [TestMethod]
    public void GetPolygon_StraightVerticalBezier3_ReturnsCorrectBoundingRectangle()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 0, y: 4, nodeName: "To");
        var edge = EdgeGenerator.Generate("From", "To", length: 4.0, variant: EdgeType.Bezier3,
            variantParameters: [new EdgeCoefficient(0, 0), new EdgeCoefficient(0, 1), new EdgeCoefficient(0, 3), new EdgeCoefficient(0, 4)]);
        var agvData = AgvDataGenerator.GenerateAgvData(length: 2.0, width: 1.0);

        var polygon = _provider.GetPolygon(edge, from, to, agvData, []);

        polygon.Points.Min(p => p.X).ShouldBe(-0.5, 1e-5);
        polygon.Points.Max(p => p.X).ShouldBe(0.5, 1e-5);
        polygon.Points.Min(p => p.Y).ShouldBe(-1.0, 1e-5);
        polygon.Points.Max(p => p.Y).ShouldBe(5.0, 1e-5); 
    }

    [TestMethod]
    public void GetPolygon_WithSafetyMargin_ExpandsPolygon()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 4, y: 0, nodeName: "To");
        var edge = EdgeGenerator.Generate("From", "To", length: 4.0, variant: EdgeType.Bezier3,
            variantParameters: [new EdgeCoefficient(0, 0), new EdgeCoefficient(1, 0), new EdgeCoefficient(3, 0), new EdgeCoefficient(4, 0)]);
        var agvData = AgvDataGenerator.GenerateAgvData(length: 2.0, width: 1.0);
        var margin = new SafetyMarginStrategy(0.1);

        var withMargin = _provider.GetPolygon(edge, from, to, agvData, [margin]);
        var withoutMargin = _provider.GetPolygon(edge, from, to, agvData, []);

        withMargin.Points.Max(p => p.Y).ShouldBeGreaterThan(withoutMargin.Points.Max(p => p.Y));
        withMargin.Points.Min(p => p.Y).ShouldBeLessThan(withoutMargin.Points.Min(p => p.Y));
    }

    [TestMethod]
    public void GetPolygon_CurvedBezier3_PolygonExtendsInCurveDirection()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 4, y: 0, nodeName: "To");
        // Arch curve: from (0,0) through control points (0,4) and (4,4) to (4,0) – sweeps above the x-axis.
        var curvedEdge = EdgeGenerator.Generate("From", "To", length: 7.0, variant: EdgeType.Bezier3,
            variantParameters: [new EdgeCoefficient(0, 0), new EdgeCoefficient(0, 4), new EdgeCoefficient(4, 4), new EdgeCoefficient(4, 0)]);
        var straightEdge = EdgeGenerator.Generate("From", "To", length: 4.0, variant: EdgeType.Bezier3,
            variantParameters: [new EdgeCoefficient(0, 0), new EdgeCoefficient(1, 0), new EdgeCoefficient(3, 0), new EdgeCoefficient(4, 0)]);
        var agvData = AgvDataGenerator.GenerateAgvData(length: 2.0, width: 1.0);

        var curvedPolygon = _provider.GetPolygon(curvedEdge, from, to, agvData, []);
        var straightPolygon = _provider.GetPolygon(straightEdge, from, to, agvData, []);

        curvedPolygon.Points.Max(p => p.Y).ShouldBeGreaterThan(straightPolygon.Points.Max(p => p.Y));
    }

    [TestMethod]
    public void GetPolygon_Bezier2_ReturnsValidPolygon()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 4, y: 0, nodeName: "To");
        // Quadratic Bezier: from (0,0), control (2,4), to (4,0) – arc above x-axis.
        var edge = EdgeGenerator.Generate("From", "To", length: 5.0, variant: EdgeType.Bezier2,
            variantParameters: [new EdgeCoefficient(0, 0), new EdgeCoefficient(2, 4), new EdgeCoefficient(4, 0)]);
        var agvData = AgvDataGenerator.GenerateAgvData(length: 2.0, width: 1.0);

        var polygon = _provider.GetPolygon(edge, from, to, agvData, []);

        polygon.Points.Count.ShouldBeGreaterThanOrEqualTo(4);
        polygon.Points.Max(p => p.Y).ShouldBeGreaterThan(0.5); // curve peaks well above AGV half-width
    }

    [TestMethod]
    public void GetPolygon_StraightBezier5_ReturnsCorrectBoundingRectangle()
    {
        var from = NodeGenerator.GenerateNode(x: 0, y: 0, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 5, y: 0, nodeName: "To");
        // Full quintic Bezier control polygon: 6 points all on the x-axis.
        var edge = EdgeGenerator.Generate("From", "To", length: 5.0, variant: EdgeType.Bezier5,
            variantParameters: [
                new EdgeCoefficient(0, 0), new EdgeCoefficient(1, 0), new EdgeCoefficient(2, 0),
                new EdgeCoefficient(3, 0), new EdgeCoefficient(4, 0), new EdgeCoefficient(5, 0)
            ]);
        var agvData = AgvDataGenerator.GenerateAgvData(length: 2.0, width: 1.0);

        var polygon = _provider.GetPolygon(edge, from, to, agvData, []);

        polygon.Points.Min(p => p.X).ShouldBe(-1.0, 1e-5);
        polygon.Points.Max(p => p.X).ShouldBe(6.0, 1e-5);
        polygon.Points.Min(p => p.Y).ShouldBe(-0.5, 1e-5);
        polygon.Points.Max(p => p.Y).ShouldBe(0.5, 1e-5);
    }

    [TestMethod]
    public void GetPolygon_ZeroLengthEdge_ReturnsPolygonWithFiniteCoordinates()
    {
        var from = NodeGenerator.GenerateNode(x: 3, y: 3, nodeName: "From");
        var to = NodeGenerator.GenerateNode(x: 3, y: 3, nodeName: "To");
        var edge = EdgeGenerator.Generate("From", "To", length: 0.0, variant: EdgeType.Bezier3,
            variantParameters: [new EdgeCoefficient(3, 3), new EdgeCoefficient(3, 3), new EdgeCoefficient(3, 3), new EdgeCoefficient(3, 3)]);
        var agvData = AgvDataGenerator.GenerateAgvData(length: 2.0, width: 1.0);

        var polygon = _provider.GetPolygon(edge, from, to, agvData, []);

        polygon.Points.ShouldAllBe(p => !double.IsNaN(p.X) && !double.IsNaN(p.Y)
                                     && !double.IsInfinity(p.X) && !double.IsInfinity(p.Y));
    }
}
