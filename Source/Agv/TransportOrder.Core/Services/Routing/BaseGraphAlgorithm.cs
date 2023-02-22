using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;

/// <summary>
/// This base class implements the basic construct of a graph algorithm
/// </summary>
public abstract class BaseGraphAlgorithm : IGraphAlgorithm
{
    private readonly IRoutingPlausibilityChecker? _routingPlausibilityChecker;

    /// <summary>List of edges coming from the AGV graph.</summary>
    protected AgvGraph.Edge[] Edges { get; }

    /// <summary>List of nodes build from the AGV graph.</summary>
    private AgvGraph.Node[] Nodes { get; }

    /// <summary>
    /// Constructor which initializes the base properties
    /// </summary>
    /// <param name="agvGraph">The agvGraph to work with</param>
    /// <param name="routingPlausibilityChecker">The checker to use for the check in the base implementation</param>
    protected BaseGraphAlgorithm(AgvGraph agvGraph, IRoutingPlausibilityChecker? routingPlausibilityChecker = null)
    {
        _routingPlausibilityChecker = routingPlausibilityChecker;
        Edges = agvGraph.Edges;
        Nodes = agvGraph.Nodes;
    }

    /// <summary>
    /// The actual implementation of the algorithm
    /// <inheritdoc cref="CalculateRoute(string,string)"/>
    /// </summary>
    protected abstract IGraphAlgorithm.Route? CalculateRouteInternal(string source, string destination);

    /// <summary>
    /// Calculate best route from <paramref name="source"/> to <paramref name="destination"/>.
    /// </summary>
    /// <param name="source">Node ID of the source.</param>
    /// <param name="destination">Node ID of the destination.</param>
    /// <returns>Returns the route as a comparable value or null when no path found.</returns>
    public IGraphAlgorithm.Route? CalculateRoute(string source, string destination)
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

        return CalculateRouteInternal(source, destination);
    }

    /// <summary>
    /// Checks whether the route is valid
    /// </summary>
    /// <param name="start">Source node</param>
    /// <param name="end">Destination node</param>
    /// <returns>True if it is plausible</returns>
    private bool IsPlausible(AgvGraph.Node start, AgvGraph.Node end)
    {
        return _routingPlausibilityChecker == null || _routingPlausibilityChecker.IsPlausible(start, end);
    }

    /// <summary>
    /// Calculate best route from <paramref name="currentNode"/> to <paramref name="destination"/> via <paramref name="source"/>.
    /// </summary>
    /// <param name="currentNode">Node ID of the current position.</param>
    /// <param name="source">Node ID of the source.</param>
    /// <param name="destination">Node ID of the destination.</param>
    /// <returns>Returns the route as a comparable value or null when no path found.</returns>
    /// <exception cref="KeyNotFoundException">Source or destination node not found.</exception>
    public virtual IGraphAlgorithm.Route? CalculateRoute(string currentNode, string source, string destination)
    {
        // Get route to source
        var routeToSource = CalculateRoute(currentNode, source);

        if (routeToSource == null)
        {
            return null;
        }
        // Get route from source to destination
        var route = CalculateRoute(source, destination);

        if (route == null)
        {
            return null;
        }

        // Return combined routes
        return routeToSource + route;
    }
}