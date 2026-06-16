using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

/// <summary>
/// Base class for edge polygon providers. Subclasses implement <see cref="GetSamples"/>. This base class applies margins on the samples and unions the results.
/// </summary>
[PublicAPI]
public abstract class EdgePolygonProviderBase : IEdgePolygonProvider
{
    /// <inheritdoc/>
    public abstract bool Supports(EdgeType edgeType);

    /// <inheritdoc/>
    public abstract double GetDirectionAtEndpoint(GraphEdge edge, GraphNode from, GraphNode to, EdgeEndpoint endpoint);

    /// <summary>
    /// Returns raw geometry samples (polygon + driving direction) for the edge, without any margins applied.
    /// </summary>
    protected abstract IReadOnlyList<(Polygon Polygon, Vector2 DrivingDirection)> GetSamples(GraphEdge edge,
        GraphNode from,
        GraphNode to,
        AgvData agvData);

    /// <inheritdoc/>
    public Polygon GetPolygon(
        GraphEdge edge,
        GraphNode from,
        GraphNode to,
        AgvData agvData,
        IReadOnlyList<IMarginStrategy> margins)
    {
        IReadOnlyList<(Polygon Polygon, Vector2 DrivingDirection)> samples = GetSamples(edge, from, to, agvData);
        List<Polygon> expanded = samples.Select(s =>
        {
            var ctx = new MarginContext(null, edge, from, to, agvData, s.DrivingDirection);
            return margins.Aggregate(s.Polygon, (p, m) => m.Apply(p, ctx));
        }).ToList();
        return Union(expanded);
    }

    private static Polygon Union(IReadOnlyList<Polygon> polygons)
    {
        return polygons.Count == 1 ? polygons[0] : throw new NotSupportedException("Multi-sample union not yet implemented.");
    }
}