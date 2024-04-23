using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Routing.Base;
using Gebhardt.Acc.TransportOrder.Routing.Models;

namespace Gebhardt.Acc.TransportOrder.Routing.Implementations;

/// <inheritdoc />
public class AStarRouteFinder : IRouteFinder
{
    /// <inheritdoc/>>
    public async Task<Route?> TryGetRouteAsync(IGraph agvGraph, AgvData? agvData = null, CancellationToken cancellationToken = default, params string[] waypoints)
    {
        var route = await Task.Run(() =>
        {
            Route? route = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var aStar = new AStar(agvGraph, agvData);
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