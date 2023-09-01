using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Microsoft.Extensions.Logging;
using TransportOrder.Domain.Exceptions;
using TransportOrder.Domain.Models.Agv;
using TransportOrder.Domain.Repositories;
using TransportOrder.Routing;
using TransportOrder.Routing.Exceptions;

namespace TransportOrder.Domain.Services.Routing;

/// <inheritdoc />
public class RouteService : IRouteService
{
    private readonly IAgvGraphRegistry _agvGraphRegistry;
    private readonly ILogger<RouteService> _logger;

    /// <inheritdoc cref="RouteService" />
    /// <param name="agvGraphRegistry"></param>
    /// <param name="logger"></param>
    public RouteService(IAgvGraphRegistry agvGraphRegistry, ILogger<RouteService> logger)
    {
        _agvGraphRegistry = agvGraphRegistry;
        _logger = logger;
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

    ///<inheritdoc />
    public async Task<GraphNode> GetNodeWithLowestCostsAsync(IAgv agv, IList<GraphNode> availableNodes)
    {
        IEnumerable<GraphNode> graphNodes = availableNodes.ToList();
        List<IGraphAlgorithm.Route> routes = await Task.WhenAll(graphNodes
                .Select(n => GetRouteIfExisting(agv, n.Id)))
                .ContinueWith(x => x.Result.Where(route => route != null)
                .Cast<IGraphAlgorithm.Route>().ToList(), TaskContinuationOptions.NotOnFaulted);

        if (!routes.Any())
        {
            throw new RouteNotFoundException(agv, availableNodes);
        }
        string lowestCostDestinationNodeId = routes.MinBy(r => r.Costs)!.Nodes.Last();

        _logger.LogDebug("Destination node with lowest cost route for Agv with AgvId {AgvId} is: {Node}. This was chosen out of: {AvailableNodes}", agv.AgvId, lowestCostDestinationNodeId, graphNodes.Select(n => n.Id).ToList());
        return graphNodes.First(n => n.Id == lowestCostDestinationNodeId);
    }
}