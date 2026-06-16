using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

/// <summary>
/// Computes the swept polygon for an edge traversed by a specific AGV type.
/// </summary>
[PublicAPI]
public interface IEdgePolygonProvider
{
    /// <summary>
    /// Returns true when this provider can handle the given edge type.
    /// </summary>
    bool Supports(EdgeType edgeType);

    /// <summary>
    /// Returns the polygon (margins applied) for edge enclosed by agvData.
    /// </summary>
    Polygon GetPolygon(GraphEdge edge, GraphNode from, GraphNode to, AgvData agvData, IReadOnlyList<IMarginStrategy> margins);

    /// <summary>
    /// Returns the AGV driving direction in radians at the given endpoint of the edge.
    /// </summary>
    double GetDirectionAtEndpoint(GraphEdge edge, GraphNode from, GraphNode to, EdgeEndpoint endpoint);
}