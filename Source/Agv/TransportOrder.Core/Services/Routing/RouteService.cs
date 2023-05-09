using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;

/// <inheritdoc />
public class RouteService : IRouteService
{
    private static string GetLastNodeId(IAgv agv) => agv.GetRequiredAgvStatus().GetRequiredLastNodeId();

    private static AgvGraph GetAgvGraph(IAgv agv) => agv.GetRequiredAgvGraph();

    private static IGraphAlgorithm.Route? GetRouteIfExisting(IAgv agv, string destinationNodeId)
    {
        string lastNodeId = GetLastNodeId(agv);
        var agvGraph = GetAgvGraph(agv);
        var route = AStar.CalculateRouteStatic(agvGraph, lastNodeId, destinationNodeId);
        return route;
    }

    private static IGraphAlgorithm.Route? GetRouteIfExisting(IAgv agv, IEnumerable<string> destinationNodeIds)
    {
        var nodes = new List<string>
        {
            GetLastNodeId(agv)
        };
        nodes.AddRange(destinationNodeIds);
        var agvGraph = GetAgvGraph(agv);

        var route = AStar.CalculateRouteStatic(agvGraph, nodes.ToArray());
        return route;
    }

    ///<inheritdoc />
    /// <exception cref="AgvLocationUnknownException"></exception>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    public bool HasRoute(IAgv agv, string destinationNodeId, out float? costs)
    {
        var route = GetRouteIfExisting(agv, destinationNodeId);
        costs = route?.Costs;
        return route != null;
    }

    /// <inheritdoc />
    public bool HasRoute(IAgv agv, IList<string> destinationNodeIds, out float? costs)
    {
        var route = GetRouteIfExisting(agv, destinationNodeIds);
        costs = route?.Costs;
        return route != null;
    }

    ///<inheritdoc />
    /// <exception cref="AgvLocationUnknownException"></exception>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    /// <exception cref="RouteNotFoundException"></exception>
    public IGraphAlgorithm.Route GetRoute(IAgv agv, string destinationNodeId)
    {
        var route = GetRouteIfExisting(agv, destinationNodeId);

        if (route == null)
        {
            throw new RouteNotFoundException(agv.AgvStatus?.LastNodeId, destinationNodeId);
        }

        return route;
    }

    ///<inheritdoc />
    /// <exception cref="AgvLocationUnknownException"></exception>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    /// <exception cref="RouteNotFoundException"></exception>
    public IGraphAlgorithm.Route GetRoute(IAgv agv, IList<string> destinationNodeIds)
    {
        var route = GetRouteIfExisting(agv, destinationNodeIds);

        if (route == null)
        {
            throw new RouteNotFoundException(agv.AgvStatus?.LastNodeId, destinationNodeIds);
        }

        return route;
    }
}