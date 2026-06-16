using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;

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
        return true;
    }

    /// <inheritdoc/>
    public override double GetDirectionAtEndpoint(GraphEdge edge, GraphNode from, GraphNode to, EdgeEndpoint endpoint)
    {
        return Math.Atan2(to.Y - (double)from.Y, to.X - (double)from.X);
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<(Polygon Polygon, Vector2 DrivingDirection)> GetSamples(GraphEdge edge,
        GraphNode from,
        GraphNode to,
        AgvData agvData)
    {
        double hL = agvData.Length / 2;
        double hW = agvData.Width / 2;
        double theta = GetDirectionAtEndpoint(edge, from, to, EdgeEndpoint.From);
        double cosT = Math.Cos(theta);
        double sinT = Math.Sin(theta);

        var polygon = new Polygon([
            new PolygonPoint(from.X - hL * cosT + hW * -sinT, from.Y - hL * sinT + hW * cosT),
            new PolygonPoint(from.X - hL * cosT - hW * -sinT, from.Y - hL * sinT - hW * cosT),
            new PolygonPoint(to.X + hL * cosT - hW * -sinT, to.Y + hL * sinT - hW * cosT),
            new PolygonPoint(to.X + hL * cosT + hW * -sinT, to.Y + hL * sinT + hW * cosT)
        ]);
        return [(polygon, new Vector2((float)cosT, (float)sinT))];
    }
}