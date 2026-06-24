using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;
using System.Numerics;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

/// <summary>
/// Treats every edge as a straight line from its start node to its end node and returns the swept rectangle.
/// </summary>
[PublicAPI]
public class DirectEdgePolygonProvider : EdgePolygonProviderBase
{
    /// <inheritdoc/>
    public override bool Supports(EdgeType edgeType)
    {
        return edgeType is EdgeType.Direct 
                        or EdgeType.Polypath or EdgeType.Slanted; // to provide a fallback for edge types that are not supported yet
    }
    
    /// <inheritdoc/>
    public override double GetDirectionAtEndpoint(GraphEdge edge, GraphNode from, GraphNode to)
    {
        double dx = to.X - (double)from.X;
        double dy = to.Y - (double)from.Y;
        return Math.Atan2(dy, dx);
    }

    /// <inheritdoc/>
    public override Polygon GetPolygon(GraphEdge edge, GraphNode from, GraphNode to, AgvData agvData, IReadOnlyList<IMarginStrategy> margins)
    {
        var samplePolygon = GetEdgeSamplePolygon(edge, agvData, margins, 0); //todo: get tangential orientation form graph model and assert it is constant for the whole edge

        (_, _, List<Vector2> startCap, List<Vector2> endCap) = SplitPolygon(samplePolygon.Points.Select(p => p.ToVector2()).ToList());

        var vector = new Vector2(to.X - from.X, to.Y - from.Y);
        var tangent = vector.LengthSquared() < Mathematics.FloatComparator ? Vector2.UnitX : Vector2.Normalize(vector);
        var normal = new Vector2(-tangent.Y, tangent.X);

        var ring = new List<Vector2>( startCap.Count + endCap.Count);

        var startFrame = (new Vector2(from.X, from.Y), tangent, normal);

        foreach (var vertex in startCap)
        {
            ring.Add(Transform(startFrame, vertex));
        }

        var endFrame = (new Vector2(to.X, to.Y), tangent, normal);

        foreach (var vertex in endCap)
        {
            ring.Add(Transform(endFrame, vertex));
        }

        List<PolygonPoint> polygonPoints = ring.Select(p => new PolygonPoint(p.X, p.Y)).ToList();

        return new Polygon(polygonPoints);
    }
}