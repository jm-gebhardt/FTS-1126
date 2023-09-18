using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Models;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Routing.Base;

/// <summary>
/// Marker interface for route finding algorithms used to calculate routes.
/// </summary>
public interface IRouteAlgorithm
{
    /// <summary>
    /// Calculate best route for the given <paramref name="waypoints"/> in the graph>
    /// Meaning it determines the route from the first element to the next and ends at the last element
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="waypoints">List of Node IDs that define the waypoints that need to be traversed on the given order</param>
    /// <returns>Returns the full aggregated route or null when no path found</returns>
    [PublicAPI]
    Route? CalculateRoute(CancellationToken cancellationToken = default, params string[] waypoints);
}