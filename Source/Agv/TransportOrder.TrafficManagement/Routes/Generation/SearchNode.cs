using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Generation;

/// <summary>
/// Represents a node in the search process for route generation.
/// </summary>
/// <param name="Predecessor">The predecessor node in the search path. Is null for the start node.</param>
/// <param name="IncomingEdge">The edge that was used to reach this node from the predecessor node. Is null for the start node.</param>
/// <param name="Node">The current routing node.</param>
/// <param name="CurrentCost">The cost from the start node to this node (g).</param>
/// <param name="TotalCost">The estimated total cost from the start node to the goal node through this node (f = g + h).</param>
internal record SearchNode(SearchNode? Predecessor, RoutingEdge? IncomingEdge, RoutingNode Node, double CurrentCost, double TotalCost);