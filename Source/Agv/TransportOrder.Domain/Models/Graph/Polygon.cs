using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Graph;

/// <summary>
/// A closed polygon defined by an ordered sequence of vertices.
/// </summary>
[PublicAPI]
public class Polygon
{
    /// <summary>
    /// The polygon vertices in CCW order.
    /// </summary>
    public IReadOnlyList<PolygonPoint> Points { get; }

    /// <inheritdoc cref="Polygon"/>
    public Polygon(IReadOnlyList<PolygonPoint> points)
    {
        Points = points;
    }

    private static bool SegmentsIntersect(PolygonPoint p1, PolygonPoint p2, PolygonPoint p3, PolygonPoint p4)
    {
        double d1 = Cross(p3.X - p4.X, p3.Y - p4.Y, p1.X - p4.X, p1.Y - p4.Y);
        double d2 = Cross(p3.X - p4.X, p3.Y - p4.Y, p2.X - p4.X, p2.Y - p4.Y);
        double d3 = Cross(p1.X - p2.X, p1.Y - p2.Y, p3.X - p2.X, p3.Y - p2.Y);
        double d4 = Cross(p1.X - p2.X, p1.Y - p2.Y, p4.X - p2.X, p4.Y - p2.Y);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
        {
            return true;
        }

        if (d1 == 0 && OnSegment(p3, p4, p1))
        {
            return true;
        }
        if (d2 == 0 && OnSegment(p3, p4, p2))
        {
            return true;
        }
        if (d3 == 0 && OnSegment(p1, p2, p3))
        {
            return true;
        }
        if (d4 == 0 && OnSegment(p1, p2, p4))
        {
            return true;
        }

        return false;
    }

    private static double Cross(double vx, double vy, double wx, double wy)
    {
        return vx * wy - vy * wx;
    }

    private static bool OnSegment(PolygonPoint a, PolygonPoint b, PolygonPoint p)
    {
        return Math.Min(a.X, b.X) <= p.X && p.X <= Math.Max(a.X, b.X) &&
               Math.Min(a.Y, b.Y) <= p.Y && p.Y <= Math.Max(a.Y, b.Y);
    }

    private static bool PointInPolygon(PolygonPoint pt, IReadOnlyList<PolygonPoint> polygon)
    {
        bool inside = false;
        int n = polygon.Count;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var vi = polygon[i];
            var vj = polygon[j];

            if (vi.Y > pt.Y != vj.Y > pt.Y &&
                pt.X < (vj.X - vi.X) * (pt.Y - vi.Y) / (vj.Y - vi.Y) + vi.X)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>
    /// Returns a new <see cref="Polygon"/> whose vertices are rotated by <paramref name="angle"/>
    /// counter-clockwise around <paramref name="center"/>. When <paramref name="center"/> is
    /// <see langword="null"/>, the rotation is performed around the origin.
    /// </summary>
    /// <param name="angle">Rotation angle in radians (counter-clockwise).</param>
    /// <param name="center">Center of rotation. Uses the origin when <see langword="null"/>.</param>
    public Polygon Rotate(double angle, PolygonPoint? center = null)
    {
        double cx = center?.X ?? 0.0;
        double cy = center?.Y ?? 0.0;
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);

        var rotated = new PolygonPoint[Points.Count];
        for (int i = 0; i < Points.Count; i++)
        {
            double dx = Points[i].X - cx;
            double dy = Points[i].Y - cy;
            rotated[i] = new PolygonPoint(cx + dx * cos - dy * sin, cy + dx * sin + dy * cos);
        }

        return new Polygon(rotated);
    }

    /// <summary>
    /// Returns true when this polygon geometrically intersects with <paramref name="other"/>.
    /// </summary>
    public bool IsIntersecting(Polygon other)
    {
        IReadOnlyList<PolygonPoint> b = other.Points;

        for (int i = 0; i < Points.Count; i++)
        {
            var a1 = Points[i];
            var a2 = Points[(i + 1) % Points.Count];

            for (int j = 0; j < b.Count; j++)
            {
                if (SegmentsIntersect(a1, a2, b[j], b[(j + 1) % b.Count]))
                {
                    return true;
                }
            }
        }

        if (PointInPolygon(Points[0], b))
        {
            return true;
        }

        if (PointInPolygon(b[0], Points))
        {
            return true;
        }

        return false;
    }
}