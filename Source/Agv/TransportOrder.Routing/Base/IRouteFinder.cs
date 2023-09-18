using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using TransportOrder.Routing.Models;

namespace TransportOrder.Routing.Base;

public interface IRouteFinder
{
    /// <summary>
    /// Calculate best route for the given <paramref name="waypoints"/> in the graph>
    /// Meaning it determines the route from the first element to the next and ends at the last element
    /// </summary>
    /// <param name="agvGraph"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="waypoints">List of Node IDs that define the waypoints that need to be traversed on the given order</param>
    /// <returns>Returns the full aggregated route or null when no path found</returns>
    Task<Route?> TryGetRouteAsync(AgvGraph agvGraph, CancellationToken cancellationToken = default, params string[] waypoints);
}