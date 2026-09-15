using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;
using AgvTypeGenerator = Gebhardt.Acc.Common.Testing.Helpers.AgvTypeGenerator;
using Fixture = Gebhardt.Acc.TransportOrder.Test.Services.Graph.DirectEdgePolygonProviderTestFixture;

namespace Gebhardt.Acc.TransportOrder.Test.Services.Graph;

[TestClass]
public class DirectEdgePolygonProviderTests
{
    [TestMethod]
    public void Supports_NoTrajectory_ReturnsTrue()
    {
        new Fixture().Provider.Supports(null).ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(Fixture.PointDegree)]
    [DataRow(Fixture.StraightDegree)]
    public void Supports_StraightTrajectory_ReturnsTrue(int degree)
    {
        new Fixture().Provider.Supports(RoutingGraphGenerator.GenerateTrajectory(degree)).ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(Fixture.QuadraticDegree)]
    [DataRow(Fixture.CubicDegree)]
    public void Supports_CurvedTrajectory_ReturnsFalse(int degree)
    {
        new Fixture().Provider.Supports(RoutingGraphGenerator.GenerateTrajectory(degree)).ShouldBeFalse();
    }

    [TestMethod]
    public void GetDirectionAtEndpoint_HorizontalEdge_ReturnsZero()
    {
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 5, 0);

        double direction = new Fixture().Provider.GetDirectionAtEndpoint(edge, edgeStartNode, edgeEndNode);

        direction.ShouldBe(0.0, 1e-9);
    }

    [TestMethod]
    public void GetDirectionAtEndpoint_VerticalEdge_ReturnsHalfPi()
    {
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 0, 5);

        double direction = new Fixture().Provider.GetDirectionAtEndpoint(edge, edgeStartNode, edgeEndNode);

        direction.ShouldBe(Math.PI / 2, 1e-9);
    }

    [TestMethod]
    public void GetDirectionAtEndpoint_DiagonalEdge_ReturnsPiOverFour()
    {
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 1, 1);

        double direction = new Fixture().Provider.GetDirectionAtEndpoint(edge, edgeStartNode, edgeEndNode);

        direction.ShouldBe(Math.PI / 4, 1e-9);
    }

    [TestMethod]
    public void GetPolygon_HorizontalEdge_ReturnsRectangleWithCorrectDimensions()
    {
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 4, 0);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);

        var polygon = new Fixture().Provider.GetPolygon(edge, edgeStartNode, edgeEndNode, agvType, []);

        polygon.Points.Count.ShouldBeGreaterThanOrEqualTo(4);
        polygon.Points.Min(p => p.X).ShouldBe(-1.0, 1e-5);
        polygon.Points.Max(p => p.X).ShouldBe(5.0, 1e-5);
        polygon.Points.Min(p => p.Y).ShouldBe(-0.5, 1e-5);
        polygon.Points.Max(p => p.Y).ShouldBe(0.5, 1e-5);
    }

    [TestMethod]
    public void GetPolygon_WithSafetyMargin_ExpandsPolygon()
    {
        var fixture = new Fixture();
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 4, 0);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);
        var margin = new SafetyMarginStrategy(0.1);

        var withMargin = fixture.Provider.GetPolygon(edge, edgeStartNode, edgeEndNode, agvType, [margin]);
        var withoutMargin = fixture.Provider.GetPolygon(edge, edgeStartNode, edgeEndNode, agvType, []);

        withMargin.Points.Max(p => p.Y).ShouldBeGreaterThan(withoutMargin.Points.Max(p => p.Y));
        withMargin.Points.Min(p => p.Y).ShouldBeLessThan(withoutMargin.Points.Min(p => p.Y));
    }

    [TestMethod]
    public void GetPolygon_VerticalEdge_SwapsDimensions()
    {
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(0, 0, 0, 4);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);

        var polygon = new Fixture().Provider.GetPolygon(edge, edgeStartNode, edgeEndNode, agvType, []);

        polygon.Points.Max(p => p.X).ShouldBe(0.5, 1e-5);
        polygon.Points.Min(p => p.X).ShouldBe(-0.5, 1e-5);
        polygon.Points.Min(p => p.Y).ShouldBe(-1.0, 1e-5);
        polygon.Points.Max(p => p.Y).ShouldBe(5.0, 1e-5);
    }

    [TestMethod]
    public void GetPolygon_ZeroLengthEdge_ReturnsPolygonWithFiniteCoordinates()
    {
        var (edge, edgeStartNode, edgeEndNode) = RoutingGraphGenerator.GenerateRoutingEdge(3, 3, 3, 3);
        var agvType = AgvTypeGenerator.GenerateAgvTypeInfo(length: Fixture.AgvLength, width: Fixture.AgvWidth);

        var polygon = new Fixture().Provider.GetPolygon(edge, edgeStartNode, edgeEndNode, agvType, []);

        polygon.Points.ShouldAllBe(p => !double.IsNaN(p.X) && !double.IsNaN(p.Y)
                                                           && !double.IsInfinity(p.X) && !double.IsInfinity(p.Y));
    }
}