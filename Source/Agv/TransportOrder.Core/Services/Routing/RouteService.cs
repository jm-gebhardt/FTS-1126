using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;

/// <inheritdoc />
public class RouteService : IRouteService
{
    private readonly IAgvGraphRegistry _agvGraphRegistry;

    /// <inheritdoc cref="RouteService" />
    /// <param name="agvGraphRegistry"></param>
    public RouteService(IAgvGraphRegistry agvGraphRegistry)
    {
        _agvGraphRegistry = agvGraphRegistry;
    }

    private static string GetLastNodeId(IAgv agv) => agv.GetRequiredAgvStatus().GetRequiredLastNodeId();

    private async Task<AgvGraph> GetAgvGraph(IAgv agv) => await _agvGraphRegistry.GetAgvGraphAsync(agv.CorrelationId);

    private async Task<IGraphAlgorithm.Route?> GetRouteIfExisting(IAgv agv, string destinationNodeId)
    {
        string lastNodeId = GetLastNodeId(agv);
        if (!await _agvGraphRegistry.ExistsAsync(agv.CorrelationId))
        {
            return null;
        }
        var agvGraph = await GetAgvGraph(agv);
        var route = AStar.CalculateRouteStatic(agvGraph, lastNodeId, destinationNodeId);
        return route!;
    }

    private async Task<IGraphAlgorithm.Route?> GetRouteIfExisting(IAgv agv, IEnumerable<string> destinationNodeIds)
    {
        var nodes = new List<string>
        {
            GetLastNodeId(agv)
        };
        nodes.AddRange(destinationNodeIds);
        var agvGraph = await GetAgvGraph(agv);

        var route = AStar.CalculateRouteStatic(agvGraph, nodes.ToArray());
        return route;
    }

    ///<inheritdoc />
    /// <exception cref="AgvLocationUnknownException"></exception>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    public bool HasRoute(IAgv agv, string destinationNodeId, out float? costs)
    {
        var route = GetRouteIfExisting(agv, destinationNodeId).Result;
        costs = route?.Costs;
        return route != null;
    }

    /// <inheritdoc />
    public bool HasRoute(IAgv agv, IList<string> destinationNodeIds, out float? costs)
    {
        var route = GetRouteIfExisting(agv, destinationNodeIds).Result;
        costs = route?.Costs;
        return route != null;
    }

    ///<inheritdoc />
    /// <exception cref="AgvLocationUnknownException"></exception>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    /// <exception cref="RouteNotFoundException"></exception>
    public async Task<IGraphAlgorithm.Route> GetRouteAsync(IAgv agv, string destinationNodeId)
    {
        var route = await GetRouteIfExisting(agv, destinationNodeId);

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
    public async Task<IGraphAlgorithm.Route> GetRouteAsync(IAgv agv, IList<string> destinationNodeIds)
    {
        var route = await GetRouteIfExisting(agv, destinationNodeIds);

        if (route == null)
        {
            throw new RouteNotFoundException(agv.AgvStatus?.LastNodeId, destinationNodeIds);
        }

        return route;
    }
}