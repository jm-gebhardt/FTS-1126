using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;
using System.Numerics;
using GraphNode = Gebhardt.Acc.Common.Domain.Models.NewGraph.Node;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

/// <summary>
/// Treats every edge as a straight line from its start node to its end node and returns the swept rectangle.
/// </summary>
[PublicAPI]
public class DirectEdgePolygonProvider : EdgePolygonProviderBase
{
    /// <inheritdoc/>
    public override bool Supports(Trajectory? trajectory)
    {
        return trajectory is null || trajectory.Degree is null || trajectory.Degree <= 1;
    }

    /// <inheritdoc/>
    public override double GetDirectionAtEndpoint(RoutingEdge edge, GraphNode startNode, GraphNode endNode)
    {
        return Math.Atan2(endNode.Y - startNode.Y, endNode.X - startNode.X);
    }

    /// <inheritdoc/>
    public override Polygon GetPolygon(RoutingEdge edge, GraphNode startNode, GraphNode endNode, IAgvType agvType, IReadOnlyList<IMarginStrategy> margins)
    {
        var samplePolygon = GetEdgeSamplePolygon(edge, agvType, margins, 0); //todo: get tangential orientation form graph model and assert it is constant for the whole edge

        var (_, _, startCap, endCap) = SplitPolygon(samplePolygon.Points.Select(p => p.ToVector2()).ToList());

        var from = new Vector2((float)startNode.X, (float)startNode.Y);
        var to = new Vector2((float)endNode.X, (float)endNode.Y);

        var vector = to - from;
        var tangent = vector.LengthSquared() < Mathematics.FloatComparator ? Vector2.UnitX : Vector2.Normalize(vector);
        var normal = new Vector2(-tangent.Y, tangent.X);

        var ring = new List<Vector2>(startCap.Count + endCap.Count);

        var startFrame = (from, tangent, normal);

        foreach (var vertex in startCap)
        {
            ring.Add(Transform(startFrame, vertex));
        }

        var endFrame = (to, tangent, normal);

        foreach (var vertex in endCap)
        {
            ring.Add(Transform(endFrame, vertex));
        }

        var polygonPoints = ring.Select(p => new PolygonPoint(p.X, p.Y)).ToList();

        return new Polygon(polygonPoints);
    }
}