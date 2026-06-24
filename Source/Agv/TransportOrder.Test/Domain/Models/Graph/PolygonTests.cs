using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models.Graph;

[TestClass]
public class PolygonTests
{
    /// <summary>Creates an axis-aligned square polygon centred at (<paramref name="cx"/>, <paramref name="cy"/>) with half-size <paramref name="h"/>.</summary>
    private static Polygon Square(double cx, double cy, double h) =>
        new([
            new PolygonPoint(cx - h, cy - h),
            new PolygonPoint(cx + h, cy - h),
            new PolygonPoint(cx + h, cy + h),
            new PolygonPoint(cx - h, cy + h)
        ]);

    [TestMethod]
    public void Constructor_StoresPoints_PointsAreAccessible()
    {
        var points = new PolygonPoint[] { new(1, 2), new(3, 4), new(5, 6) };

        var polygon = new Polygon(points);

        polygon.Points.Count.ShouldBe(3);
        polygon.Points[0].X.ShouldBe(1);
        polygon.Points[0].Y.ShouldBe(2);
    }

    [TestMethod]
    public void Rotate_ZeroAngle_ReturnsPolygonWithUnchangedVertices()
    {
        var polygon = Square(0, 0, 1);

        var rotated = polygon.Rotate(0);

        for (int i = 0; i < polygon.Points.Count; i++)
        {
            rotated.Points[i].X.ShouldBe(polygon.Points[i].X, 1e-9);
            rotated.Points[i].Y.ShouldBe(polygon.Points[i].Y, 1e-9);
        }
    }

    [TestMethod]
    public void Rotate_QuarterTurnCCW_VertexLandsAtExpectedPosition()
    {
        // (1, 0) rotated 90° CCW around the origin → (0, 1)
        var polygon = new Polygon([new PolygonPoint(1, 0), new PolygonPoint(1, 1), new PolygonPoint(0, 1)]);

        var rotated = polygon.Rotate(Math.PI / 2);

        rotated.Points[0].X.ShouldBe(0, 1e-9);
        rotated.Points[0].Y.ShouldBe(1, 1e-9);
    }

    [TestMethod]
    public void Rotate_FullCircle_ReturnsPolygonWithOriginalVertices()
    {
        var polygon = Square(1, 2, 1);

        var rotated = polygon.Rotate(2 * Math.PI);

        for (int i = 0; i < polygon.Points.Count; i++)
        {
            rotated.Points[i].X.ShouldBe(polygon.Points[i].X, 1e-9);
            rotated.Points[i].Y.ShouldBe(polygon.Points[i].Y, 1e-9);
        }
    }

    [TestMethod]
    public void Rotate_AroundCustomCenter_VertexLandsAtExpectedPosition()
    {
        // (2, 0) rotated 90° CCW around center (1, 0):
        //   dx=1, dy=0 → x'=1+0=1, y'=0+1=1  →  result (1, 1)
        var polygon = new Polygon([new PolygonPoint(2, 0), new PolygonPoint(2, 1), new PolygonPoint(1, 1)]);

        var rotated = polygon.Rotate(Math.PI / 2, new PolygonPoint(1, 0));

        rotated.Points[0].X.ShouldBe(1, 1e-9);
        rotated.Points[0].Y.ShouldBe(1, 1e-9);
    }

    [TestMethod]
    public void Rotate_DoesNotModifyOriginalPolygon()
    {
        var polygon = Square(0, 0, 1);
        double originalX = polygon.Points[0].X;
        double originalY = polygon.Points[0].Y;

        _ = polygon.Rotate(Math.PI / 3);

        polygon.Points[0].X.ShouldBe(originalX);
        polygon.Points[0].Y.ShouldBe(originalY);
    }

    [TestMethod]
    public void IsIntersecting_OverlappingSquares_ReturnsTrue()
    {
        var a = Square(0, 0, 1);  
        var b = Square(1, 0, 1);

        a.IsIntersecting(b).ShouldBeTrue();
    }

    [TestMethod]
    public void IsIntersecting_NonOverlappingSquares_ReturnsFalse()
    {
        var a = Square(0, 0, 1);
        var b = Square(4, 0, 1);

        a.IsIntersecting(b).ShouldBeFalse();
    }

    [TestMethod]
    public void IsIntersecting_OneInsideOther_ReturnsTrue()
    {
        var outer = Square(0, 0, 3);
        var inner = Square(0, 0, 1);

        outer.IsIntersecting(inner).ShouldBeTrue();
    }

    [TestMethod]
    public void IsIntersecting_PolygonWithItself_ReturnsTrue()
    {
        var polygon = Square(0, 0, 1);

        polygon.IsIntersecting(polygon).ShouldBeTrue();
    }

    [TestMethod]
    public void IsIntersecting_EdgeTouching_ReturnsTrue()
    {
        var a = Square(0, 0, 1);
        var b = Square(2, 0, 1);

        a.IsIntersecting(b).ShouldBeTrue();
    }

    [TestMethod]
    public void IsIntersecting_IsSymmetric_BothDirectionsReturnSameResult()
    {
        var a = Square(0, 0, 1);
        var b = Square(0.5, 0, 1);

        a.IsIntersecting(b).ShouldBe(b.IsIntersecting(a));
    }
}
