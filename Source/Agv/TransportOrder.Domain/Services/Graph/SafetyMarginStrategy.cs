using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

/// <summary>
/// Expands every side of a polygon outward by a fixed distance.
/// </summary>
[PublicAPI]
public class SafetyMarginStrategy : IMarginStrategy
{
    private readonly double _marginMeters;

    /// <inheritdoc cref="SafetyMarginStrategy"/>
    public SafetyMarginStrategy(double marginMeters)
    {
        _marginMeters = marginMeters;
    }

    private static Vector2 Normalize(double dx, double dy)
    {
        double len = Math.Sqrt(dx * dx + dy * dy);
        return len < 1e-10 ? Vector2.Zero : new Vector2((float)(dx / len), (float)(dy / len));
    }

    /// <inheritdoc/>
    public Polygon Apply(Polygon polygon)
    {
        int count = polygon.Points.Count;
        var result = new PolygonPoint[count];
        for (int i = 0; i < count; i++)
        {
            var prev = polygon.Points[(i - 1 + count) % count];
            var curr = polygon.Points[i];
            var next = polygon.Points[(i + 1) % count];

            var eIn = Normalize(curr.X - prev.X, curr.Y - prev.Y);
            var eOut = Normalize(next.X - curr.X, next.Y - curr.Y);

            // Outward normals for a CCW polygon (right-hand side of each edge's travel direction).
            var nIn = new Vector2(eIn.Y, -eIn.X);
            var nOut = new Vector2(eOut.Y, -eOut.X);

            var bisector = Vector2.Normalize(nIn + nOut);
            double sinHalfAngle = Vector2.Dot(bisector, nIn);
            double scale = _marginMeters / sinHalfAngle;

            result[i] = new PolygonPoint(curr.X + scale * bisector.X, curr.Y + scale * bisector.Y);
        }

        return new Polygon(result);
    }
}