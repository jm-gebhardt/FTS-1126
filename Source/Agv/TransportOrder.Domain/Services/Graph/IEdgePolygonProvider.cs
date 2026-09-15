using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;
using GraphNode = Gebhardt.Acc.Common.Domain.Models.NewGraph.Node;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

/// <summary>
/// Computes the swept polygon for an edge traversed by a specific AGV type.
/// </summary>
[PublicAPI]
public interface IEdgePolygonProvider
{
    /// <summary>
    /// Returns true when this provider can handle the given trajectory.
    /// </summary>
    bool Supports(Trajectory? trajectory);

    /// <summary>
    /// Returns the polygon (margins applied) swept by the given AGV type along the edge.
    /// </summary>
    Polygon GetPolygon(RoutingEdge edge, GraphNode startNode, GraphNode endNode, IAgvType agvType, IReadOnlyList<IMarginStrategy> margins);

    /// <summary>
    /// Returns the AGV driving direction in radians at the given endpoint of the edge.
    /// </summary>
    double GetDirectionAtEndpoint(RoutingEdge edge, GraphNode startNode, GraphNode endNode);
}