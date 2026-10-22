using Gebhardt.Acc.TransportOrder.TrafficManagement.Agvs;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Manager;

/// <summary>
/// Data required for the invocation or update of a route for an AGV.
/// </summary>
/// <param name="Agv">The AGV.</param>
/// <param name="TargetNodeCandidates">The target node candidates.</param>
/// <param name="ActionCorrelationId">The correlation ID for the action associated with the route.</param>
internal record RouteRequest(RoutingAgv Agv, IReadOnlyCollection<RoutingNode> TargetNodeCandidates, Guid ActionCorrelationId)
{
    /// <summary>
    /// The whitelisted action edges that are allowed for the route.
    /// </summary>
    public IReadOnlyCollection<RoutingEdge> WhitelistedActionEdges { get; init; } = [];
}