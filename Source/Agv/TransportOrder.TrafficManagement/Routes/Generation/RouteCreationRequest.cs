using Gebhardt.Acc.TransportOrder.TrafficManagement.Agvs;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Generation;

/// <summary>
/// A request to create a route for an AGV with RouteGenerator.
/// </summary>
internal record RouteCreationRequest
{
    /// <summary>
    /// The AGV for which the route is being created.
    /// </summary>
    public RoutingAgv Agv { get; }
    
    /// <summary>
    /// The starting node for the route creation.
    /// </summary>
    public RoutingNode StartNode { get; }

    /// <summary>
    /// The set of candidate target nodes for the route creation.
    /// </summary>
    public IReadOnlySet<RoutingNode> TargetNodeCandidates { get; }

    /// <summary>
    /// A unique identifier for the associated action.
    /// </summary>
    public Guid ActionCorrelationId { get; }

    /// <summary>
    /// The set of whitelisted action edges for the route creation.
    /// </summary>
    public IReadOnlySet<RoutingEdge> WhitelistedActionEdges { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteCreationRequest"/> class.
    /// </summary>
    public RouteCreationRequest(
        RoutingAgv agv, 
        RoutingNode startNode, 
        IEnumerable<RoutingNode> targetNodeCandidates, 
        Guid actionCorrelationId, 
        IEnumerable<RoutingEdge> whitelistedActionEdges)
    {
        Agv = agv;
        StartNode = startNode;
        TargetNodeCandidates = targetNodeCandidates.ToHashSet();
        ActionCorrelationId = actionCorrelationId;
        WhitelistedActionEdges = whitelistedActionEdges.ToHashSet();
    }

    /// <summary>
    /// Validates the route creation request to ensure that it contains all required information and has no contradictions.
    /// </summary>
    /// <returns>True if the request is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        if (TargetNodeCandidates.Count == 0)
        {
            return false;
        }

        return true;
    }
}