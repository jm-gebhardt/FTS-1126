using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;
using AgvTypeGenerator = Gebhardt.Acc.Common.Testing.Helpers.AgvTypeGenerator;
using Fixture = Gebhardt.Acc.TransportOrder.Test.Services.Graph.BezierEdgePolygonProviderTestFixture;

namespace Gebhardt.Acc.TransportOrder.Test.Services.Graph;

[TestClass]
public class BezierEdgePolygonProviderTests
{
    [TestMethod]
    [DataRow(Fixture.QuadraticDegree)]
    [DataRow(Fixture.CubicDegree)]
    [DataRow(Fixture.QuinticDegree)]
    public void Supports_CurvedTrajectory_ReturnsTrue(int degree)
    {
        new Fixture().Provider.Supports(RoutingGraphGenerator.GenerateTrajectory(degree)).ShouldBeTrue();
    }

    [TestMethod]
    public void Supports_NoTrajectory_ReturnsFalse()
    {
        new Fixture().Provider.Supports(null).ShouldBeFalse();
    }

    [TestMethod]
    public void Supports_StraightTrajectory_ReturnsFalse()
    {
        new Fixture().Provider.Supports(RoutingGraphGenerator.GenerateTrajectory(Fixture.StraightDegree)).ShouldBeFalse();
    }

    [TestMethod]
    public void GetDirectionAtEndpoint_StraightHorizontalEdge_ReturnsZero()
    {
        var trajectory = RoutingGraphGenerator.GenerateTrajectory(Fixture.CubicDegree,
            new PolygonPoint(0, 0), new PolygonPoint(1, 0), new PolygonPoint(3, 0), new PolygonPoint(4, 0));
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 4, 0, 4.0, trajectory);

        double direction = new Fixture().Provider.GetDirectionAtEndpoint(edge, edgeStartNode, edgeEndNode);

        direction.ShouldBe(0.0, 1e-5);
    }

    [TestMethod]
    public void GetDirectionAtEndpoint_StraightVerticalEdge_ReturnsHalfPi()
    {
        var trajectory = RoutingGraphGenerator.GenerateTrajectory(Fixture.CubicDegree,
            new PolygonPoint(0, 0), new PolygonPoint(0, 1), new PolygonPoint(0, 3), new PolygonPoint(0, 4));
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 0, 4, 4.0, trajectory);

        double direction = new Fixture().Provider.GetDirectionAtEndpoint(edge, edgeStartNode, edgeEndNode);

        direction.ShouldBe(Math.PI / 2, 1e-5);
    }

    [TestMethod]
    public void GetDirectionAtEndpoint_StraightDiagonalEdge_ReturnsPiOverFour()
    {
        var trajectory = RoutingGraphGenerator.GenerateTrajectory(Fixture.CubicDegree,
            new PolygonPoint(0, 0), new PolygonPoint(1, 1), new PolygonPoint(2, 2), new PolygonPoint(3, 3));
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 3, 3, 3 * Math.Sqrt(2), trajectory);

        double direction = new Fixture().Provider.GetDirectionAtEndpoint(edge, edgeStartNode, edgeEndNode);

        direction.ShouldBe(Math.PI / 4, 1e-4);
    }

    [TestMethod]
    public void GetPolygon_StraightHorizontalBezier3_ReturnsCorrectBoundingRectangle()
    {
        var trajectory = RoutingGraphGenerator.GenerateTrajectory(Fixture.CubicDegree,
            new PolygonPoint(0, 0), new PolygonPoint(1, 0), new PolygonPoint(3, 0), new PolygonPoint(4, 0));
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 4, 0, 4.0, trajectory);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);

        var polygon = new Fixture().Provider.GetPolygon(edge, edgeStartNode, edgeEndNode, agvType, []);

        polygon.Points.Min(p => p.X).ShouldBe(-1.0, 1e-5);
        polygon.Points.Max(p => p.X).ShouldBe(5.0, 1e-5);
        polygon.Points.Min(p => p.Y).ShouldBe(-0.5, 1e-5);
        polygon.Points.Max(p => p.Y).ShouldBe(0.5, 1e-5);
    }

    [TestMethod]
    public void GetPolygon_StraightBezier3_CollinearPointsRemoved_PolygonHasFourCorners()
    {
        var trajectory = RoutingGraphGenerator.GenerateTrajectory(Fixture.CubicDegree,
            new PolygonPoint(0, 0), new PolygonPoint(1, 0), new PolygonPoint(3, 0), new PolygonPoint(4, 0));
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 4, 0, 4.0, trajectory);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);

        var polygon = new Fixture().Provider.GetPolygon(edge, edgeStartNode, edgeEndNode, agvType, []);

        polygon.Points.Count.ShouldBe(4);
    }

    [TestMethod]
    public void GetPolygon_StraightVerticalBezier3_ReturnsCorrectBoundingRectangle()
    {
        var trajectory = RoutingGraphGenerator.GenerateTrajectory(Fixture.CubicDegree,
            new PolygonPoint(0, 0), new PolygonPoint(0, 1), new PolygonPoint(0, 3), new PolygonPoint(0, 4));
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 0, 4, 4.0, trajectory);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);

        var polygon = new Fixture().Provider.GetPolygon(edge, edgeStartNode, edgeEndNode, agvType, []);

        polygon.Points.Min(p => p.X).ShouldBe(-0.5, 1e-5);
        polygon.Points.Max(p => p.X).ShouldBe(0.5, 1e-5);
        polygon.Points.Min(p => p.Y).ShouldBe(-1.0, 1e-5);
        polygon.Points.Max(p => p.Y).ShouldBe(5.0, 1e-5);
    }

    [TestMethod]
    public void GetPolygon_WithSafetyMargin_ExpandsPolygon()
    {
        var fixture = new Fixture();
        var trajectory = RoutingGraphGenerator.GenerateTrajectory(Fixture.CubicDegree,
            new PolygonPoint(0, 0), new PolygonPoint(1, 0), new PolygonPoint(3, 0), new PolygonPoint(4, 0));
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 4, 0, 4.0, trajectory);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);
        var margin = new SafetyMarginStrategy(0.1);

        var withMargin = fixture.Provider.GetPolygon(edge, edgeStartNode, edgeEndNode, agvType, [margin]);
        var withoutMargin = fixture.Provider.GetPolygon(edge, edgeStartNode, edgeEndNode, agvType, []);

        withMargin.Points.Max(p => p.Y).ShouldBeGreaterThan(withoutMargin.Points.Max(p => p.Y));
        withMargin.Points.Min(p => p.Y).ShouldBeLessThan(withoutMargin.Points.Min(p => p.Y));
    }

    [TestMethod]
    public void GetPolygon_CurvedBezier3_PolygonExtendsInCurveDirection()
    {
        var fixture = new Fixture();
        var curvedTrajectory = RoutingGraphGenerator.GenerateTrajectory(Fixture.CubicDegree,
            new PolygonPoint(0, 0), new PolygonPoint(0, 4), new PolygonPoint(4, 4), new PolygonPoint(4, 0));
        var straightTrajectory = RoutingGraphGenerator.GenerateTrajectory(Fixture.CubicDegree,
            new PolygonPoint(0, 0), new PolygonPoint(1, 0), new PolygonPoint(3, 0), new PolygonPoint(4, 0));
        var (curvedEdge, curvedStartNode, curvedEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 4, 0, 7.0, curvedTrajectory);
        var (straightEdge, straightStartNode, straightEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 4, 0, 4.0, straightTrajectory);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);

        var curvedPolygon = fixture.Provider.GetPolygon(curvedEdge, curvedStartNode, curvedEndNode, agvType, []);
        var straightPolygon = fixture.Provider.GetPolygon(straightEdge, straightStartNode, straightEndNode, agvType, []);

        curvedPolygon.Points.Max(p => p.Y).ShouldBeGreaterThan(straightPolygon.Points.Max(p => p.Y));
    }

    [TestMethod]
    public void GetPolygon_Bezier2_ReturnsValidPolygon()
    {
        var trajectory = RoutingGraphGenerator.GenerateTrajectory(Fixture.QuadraticDegree,
            new PolygonPoint(0, 0), new PolygonPoint(2, 4), new PolygonPoint(4, 0));
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 4, 0, 5.0, trajectory);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);

        var polygon = new Fixture().Provider.GetPolygon(edge, edgeStartNode, edgeEndNode, agvType, []);

        polygon.Points.Count.ShouldBeGreaterThanOrEqualTo(4);
        polygon.Points.Max(p => p.Y).ShouldBeGreaterThan(0.5);
    }

    [TestMethod]
    public void GetPolygon_StraightBezier5_ReturnsCorrectBoundingRectangle()
    {
        var trajectory = RoutingGraphGenerator.GenerateTrajectory(Fixture.QuinticDegree,
            new PolygonPoint(0, 0), new PolygonPoint(1, 0), new PolygonPoint(2, 0),
            new PolygonPoint(3, 0), new PolygonPoint(4, 0), new PolygonPoint(5, 0));
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 5, 0, 5.0, trajectory);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);

        var polygon = new Fixture().Provider.GetPolygon(edge, edgeStartNode, edgeEndNode, agvType, []);

        polygon.Points.Min(p => p.X).ShouldBe(-1.0, 1e-5);
        polygon.Points.Max(p => p.X).ShouldBe(6.0, 1e-5);
        polygon.Points.Min(p => p.Y).ShouldBe(-0.5, 1e-5);
        polygon.Points.Max(p => p.Y).ShouldBe(0.5, 1e-5);
    }

    [TestMethod]
    public void GetPolygon_ZeroLengthEdge_ReturnsPolygonWithFiniteCoordinates()
    {
        var trajectory = RoutingGraphGenerator.GenerateTrajectory(Fixture.CubicDegree,
            new PolygonPoint(3, 3), new PolygonPoint(3, 3), new PolygonPoint(3, 3), new PolygonPoint(3, 3));
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(3, 3, 3, 3, 0.0, trajectory);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);

        var polygon = new Fixture().Provider.GetPolygon(edge, edgeStartNode, edgeEndNode, agvType, []);

        polygon.Points.ShouldAllBe(p => !double.IsNaN(p.X) && !double.IsNaN(p.Y)
                                                           && !double.IsInfinity(p.X) && !double.IsInfinity(p.Y));
    }
}