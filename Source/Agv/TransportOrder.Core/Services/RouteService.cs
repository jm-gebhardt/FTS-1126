 using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;

/// <inheritdoc />
public class RouteService : IRouteService
{
    ///<inheritdoc />
    /// <exception cref="AgvLocationUnknownException"></exception>
    public bool HasRoute(IAgv agv, string destination, out float? costs)
    {
        string lastNodeId = agv.AgvStatus!.LastNodeId!;
        if (lastNodeId == null)
        {
            throw new AgvLocationUnknownException(agv);
        }
        var route = AStar.CalculateRouteStatic(agv.AgvGraph!, lastNodeId, destination);
        costs = route?.Costs;
        return route != null;
    }
}