using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Routing.Base;

/// <summary>
/// Plausibility checker for a route (node-node)
/// </summary>
public interface IRoutingPlausibilityChecker
{
    /// <summary>
    /// Checks whether this route is plausible
    /// <inheritdoc cref="IRoutingPlausibilityChecker"/>
    /// </summary>
    /// <param name="start">The source node of the route</param>
    /// <param name="end">The destination node of the route</param>
    /// <returns>True if it is plausible</returns>
    bool IsPlausible(GraphNode start, GraphNode end);
}