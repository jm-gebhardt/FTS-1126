using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Models;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Routing.Base;

/// <summary>
/// This base class implements the basic construct of a graph algorithm
/// </summary>
public abstract class BaseRouteAlgorithm : IRouteAlgorithm
{
    /// <summary>
    /// The fallback value for max non spot turn angle.
    /// </summary>
    protected const double MaxNonSpotTurnAngleFallback = 5 * Math.PI / 180d;

    /// <summary>
    /// The fallback value for max velocity straight.
    /// </summary>
    protected const double MaxVelocityStraightFallback = 1;

    /// <summary>
    /// The fallback value for max velocity turn on spot.
    /// </summary>
    protected const double MaxVelocityTurnOnSpotFallback = Math.PI;

    /// <summary>
    /// The fallback values or acceleration.
    /// </summary>
    protected const double AccelerationFallback = 1;

    /// <summary>
    /// The fallback value for deceleration.
    /// </summary>
    protected const double MaxVelocityTurnFallback = 1;

    private readonly IRoutingPlausibilityChecker? _routingPlausibilityChecker;

    /// <summary>List of edges coming from the AGV graph.</summary>
    protected ICollection<GraphEdge> Edges { get; }

    /// <summary>List of nodes build from the AGV graph.</summary>
    private ICollection<GraphNode> Nodes { get; }

    /// <summary>
    /// Constructor which initializes the base properties
    /// </summary>
    /// <param name="agvGraph">The agvGraph to work with</param>
    /// <param name="routingPlausibilityChecker">The checker to use for the check in the base implementation</param>
    protected BaseRouteAlgorithm(IGraph agvGraph, IRoutingPlausibilityChecker? routingPlausibilityChecker = null)
    {
        _routingPlausibilityChecker = routingPlausibilityChecker;
        Edges = agvGraph.Edges;
        Nodes = agvGraph.Nodes;
    }

    /// <summary>
    /// The actual implementation of the algorithm
    /// <inheritdoc cref="CalculateRoute(string,string)"/>
    /// </summary>
    protected abstract Route? CalculateRouteInternal(string source, string destination);

    private Route? CalculateRoute(string source, string destination)
    {
        var start = Nodes.SingleOrDefault(n => n.Id.Equals(source));
        var end = Nodes.SingleOrDefault(n => n.Id.Equals(destination));

        if (start == null || end == null)
        {
            return null;
        }

        if (!IsPlausible(start, end))
        {
            return null;
        }

        if (source == destination) // Already at destination -> 0 cost
        {
            return new Route(0f, new[] {source});
        }

        return CalculateRouteInternal(source, destination);
    }

    /// <summary>
    /// Checks whether the route is valid
    /// </summary>
    /// <param name="start">Source node</param>
    /// <param name="end">Destination node</param>
    /// <returns>True if it is plausible</returns>
    private bool IsPlausible(GraphNode start, GraphNode end)
    {
        return _routingPlausibilityChecker == null || _routingPlausibilityChecker.IsPlausible(start, end);
    }

    /// <inheritdoc cref="IRouteAlgorithm.CalculateRoute(CancellationToken, string[])"/>
    public Route? CalculateRoute(params string[] waypoints)
    {
        return CalculateRoute(default, waypoints);
    }

    /// <inheritdoc/>
    /// <exception cref="KeyNotFoundException">A waypoint node was not found.</exception>
    public Route? CalculateRoute(CancellationToken cancellationToken = default, params string[] waypoints)
    {
        if (waypoints.Length < 2)
        {
            return null;
        }

        var totalRoute = new Route(0, Array.Empty<string>());

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            string source = waypoints[i];
            string destination = waypoints[i + 1];

            cancellationToken.ThrowIfCancellationRequested();
            var route = CalculateRoute(source, destination);
            if (route == null)
            {
                return null;
            }

            totalRoute += route;
        }

        return totalRoute;
    }
}