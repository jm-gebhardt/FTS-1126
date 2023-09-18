using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using TransportOrder.Routing.Base;
using TransportOrder.Routing.Models;

namespace TransportOrder.Routing.Implementations;

public class AStarRouteFinder : IRouteFinder
{
    /// <inheritdoc/>>
    public async Task<Route?> TryGetRouteAsync(AgvGraph agvGraph, CancellationToken cancellationToken = default, params string[] waypoints)
    {
        var route = await Task.Run(() =>
        {
            Route? route = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var aStar = new AStar(agvGraph);
                cancellationToken.ThrowIfCancellationRequested();
                route = aStar.CalculateRoute(cancellationToken, waypoints);
            }
            catch
            {
                // Method name is "Try", so silently catch any exception and return null
            }

            return route;
        }, cancellationToken).ConfigureAwait(false);

        return route;
    }
}