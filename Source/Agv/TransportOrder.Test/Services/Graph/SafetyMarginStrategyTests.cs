using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.Graph;

[TestClass]
public class SafetyMarginStrategyTests
{
    [TestMethod]
    public void Apply_SquarePolygon_ExpandsAllSidesUniformly()
    {
        const double margin = 0.2;
        var strategy = new SafetyMarginStrategy(margin);

        var polygon = new Polygon([
            new PolygonPoint(-1, -1),
            new PolygonPoint( 1, -1),
            new PolygonPoint( 1,  1),
            new PolygonPoint(-1,  1)
        ]);

        var expanded = strategy.Apply(polygon);

        expanded.Points.Count.ShouldBe(4);
        expanded.Points.Max(p => p.X).ShouldBe(1.0 + margin, 1e-6);
        expanded.Points.Min(p => p.X).ShouldBe(-1.0 - margin, 1e-6);
        expanded.Points.Max(p => p.Y).ShouldBe(1.0 + margin, 1e-6);
        expanded.Points.Min(p => p.Y).ShouldBe(-1.0 - margin, 1e-6);
    }

    [TestMethod]
    public void Apply_ZeroMargin_ReturnsUnchangedPolygon()
    {
        var strategy = new SafetyMarginStrategy(0.0);

        var polygon = new Polygon([
            new PolygonPoint(-1, -1),
            new PolygonPoint( 1, -1),
            new PolygonPoint( 1,  1),
            new PolygonPoint(-1,  1)
        ]);

        var expanded = strategy.Apply(polygon);

        expanded.Points.Count.ShouldBe(4);
        for (int i = 0; i < 4; i++)
        {
            expanded.Points[i].X.ShouldBe(polygon.Points[i].X, 1e-6);
            expanded.Points[i].Y.ShouldBe(polygon.Points[i].Y, 1e-6);
        }
    }

    [TestMethod]
    public void Apply_ExpandedPolygon_ContainsOriginalPolygon()
    {
        var strategy = new SafetyMarginStrategy(0.5);

        var polygon = new Polygon([
            new PolygonPoint(-1, -1),
            new PolygonPoint( 1, -1),
            new PolygonPoint( 1,  1),
            new PolygonPoint(-1,  1)
        ]);

        var expanded = strategy.Apply(polygon);

        // The bounding box of the expanded polygon must be larger than the original
        expanded.Points.Max(p => p.X).ShouldBeGreaterThan(polygon.Points.Max(p => p.X));
        expanded.Points.Min(p => p.X).ShouldBeLessThan(polygon.Points.Min(p => p.X));
        expanded.Points.Max(p => p.Y).ShouldBeGreaterThan(polygon.Points.Max(p => p.Y));
        expanded.Points.Min(p => p.Y).ShouldBeLessThan(polygon.Points.Min(p => p.Y));
    }
}
