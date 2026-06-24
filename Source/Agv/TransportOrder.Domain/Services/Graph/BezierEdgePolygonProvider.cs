using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using System.Numerics;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

/// <summary>
/// An edge polygon provider for Bezier edges, when tangential orientation is constant. Sweeps a convex bounding box polygon tangentially along the curve and returns a single enclosing
/// polygon whose outline is traced directly from the swept band (swept boundary tracing).
/// </summary>
public class BezierEdgePolygonProvider : EdgePolygonProviderBase
{
    private const double StepSize = 0.1;

    /// <inheritdoc/>
    public override bool Supports(EdgeType edgeType)
    {
        return edgeType is EdgeType.Bezier2 or EdgeType.Bezier3 or EdgeType.Bezier5;
    }

    /// <inheritdoc/>
    public override double GetDirectionAtEndpoint(GraphEdge edge, GraphNode from, GraphNode to)
    {
        var bezier = new Bezier(edge.VariantParameters.Select(cp => cp.ToVector2()).ToList());

        (Vector2 Point, Vector2 Tangent, Vector2 Normal)[] frames = bezier.GetSamples(2);

        var tangentAtEnd = frames[1].Tangent;

        return Math.Atan2(tangentAtEnd.Y, tangentAtEnd.X);
    }

    /// <inheritdoc/>
    public override Polygon GetPolygon(GraphEdge edge, GraphNode from, GraphNode to, AgvData agvData, IReadOnlyList<IMarginStrategy> margins)
    {
        int steps = Math.Max((int)Math.Ceiling(edge.Length / StepSize), 2);

        var samplePolygon = GetEdgeSamplePolygon(edge, agvData, margins, 0); //todo: get tangential orientation form graph model and assert it is constant for the whole edge

        (var innerVertex, var outerVertex, List<Vector2> startCap, List<Vector2> endCap) = SplitPolygon(samplePolygon.Points.Select(p => p.ToVector2()).ToList());

        var bezier = new Bezier(edge.VariantParameters.Select(cp => cp.ToVector2()).ToList());

        (Vector2 Point, Vector2 Tangent, Vector2 Normal)[] frames = bezier.GetSamples(steps);

        var ring = new List<Vector2>(steps * 2 + startCap.Count + endCap.Count);

        // Inner rail forward.
        foreach (var frame in frames)
        {
            ring.Add(Transform(frame, innerVertex));
        }
        // Front cap at the last step (inner -> outer).
        foreach (var vertex in endCap)
        {
            ring.Add(Transform(frames[^1], vertex));
        }
        // Outer rail backward.
        for (int i = steps - 1; i >= 0; i--)
        {
            ring.Add(Transform(frames[i], outerVertex));
        }
        // Rear cap at the first step (outer -> inner).
        foreach (var vertex in startCap)
        {
            ring.Add(Transform(frames[0], vertex));
        }

        List<Vector2> outline = RemoveCollinearPoints(RemoveConsecutiveDuplicates(ring));

        List<PolygonPoint> polygonPoints = outline.Select(p => new PolygonPoint(p.X, p.Y)).ToList();

        return new Polygon(polygonPoints);
    }

    /// <summary>
    /// Removes consecutive duplicate points and the closing duplicate from a polygon ring.
    /// </summary>
    private static List<Vector2> RemoveConsecutiveDuplicates(IReadOnlyList<Vector2> ring)
    {
        var result = new List<Vector2>(ring.Count);

        foreach (var point in ring)
        {
            if (result.Count == 0 || Vector2.DistanceSquared(result[^1], point) > Mathematics.FloatComparator)
            {
                result.Add(point);
            }
        }

        if (result.Count > 1 && Vector2.DistanceSquared(result[0], result[^1]) <= Mathematics.FloatComparator)
        {
            result.RemoveAt(result.Count - 1);
        }
        return result;
    }

    /// <summary>
    /// Removes points that lie on the straight line between their neighbours so only the corner vertices remain.
    /// </summary>
    private static List<Vector2> RemoveCollinearPoints(List<Vector2> polygon)
    {
        if (polygon.Count < 3)
        {
            return polygon;
        }

        var result = new List<Vector2>(polygon.Count);
        for (int i = 0; i < polygon.Count; i++)
        {
            var previous = polygon[(i - 1 + polygon.Count) % polygon.Count];
            var current = polygon[i];
            var next = polygon[(i + 1) % polygon.Count];

            double cross = ((double)current.X - previous.X) * ((double)next.Y - previous.Y)
                           - ((double)current.Y - previous.Y) * ((double)next.X - previous.X);
            if (Math.Abs(cross) > Mathematics.FloatComparator)
            {
                result.Add(current);
            }
        }

        return result.Count >= 3 ? result : polygon;
    }
}