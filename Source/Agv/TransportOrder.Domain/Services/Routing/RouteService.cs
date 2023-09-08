using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Microsoft.Extensions.Logging;
using TransportOrder.Domain.Exceptions;
using TransportOrder.Domain.Models.ActionGroup;
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

    private async Task<IGraphAlgorithm.Route?> GetRouteIfExistingAsync(IAgv agv, string destinationNodeId, CancellationToken cancellationToken = default)
    {
        string lastNodeId = GetLastNodeId(agv);

        if (!await _agvGraphRegistry.ExistsAsync(agv.CorrelationId))
        {
            return null;
        }

        // Checking for cancellation before proceeding
        cancellationToken.ThrowIfCancellationRequested();

        var agvGraph = await GetAgvGraph(agv);

        cancellationToken.ThrowIfCancellationRequested();

        var route = AStar.CalculateRouteStatic(agvGraph, lastNodeId, destinationNodeId);
        return route!;
    }

    private async Task<IGraphAlgorithm.Route?> GetRouteIfExistingAsync(IAgv agv, IEnumerable<string> destinationNodeIds, CancellationToken cancellationToken = default)
    {
        var nodes = new List<string>
        {
            GetLastNodeId(agv)
        };
        nodes.AddRange(destinationNodeIds);
        var agvGraph = await GetAgvGraph(agv);
        cancellationToken.ThrowIfCancellationRequested();

        var route = AStar.CalculateRouteStatic(agvGraph, nodes.ToArray());
        return route;
    }

    ///<inheritdoc />
    /// <exception cref="AgvLocationUnknownException"></exception>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    public async Task<float?> GetCostsAsync(IAgv agv, string destinationNodeId, CancellationToken cancellationToken = default)
    {
        var route = await GetRouteIfExistingAsync(agv, destinationNodeId, cancellationToken);
        return route?.Costs;
    }

    /// <inheritdoc />
    public async Task<float?> GetCostsForFirstActionAsync(IAgv agv, Models.ActionGroup.ActionGroup actionGroup, CancellationToken cancellationToken = default)
    {
        AgvAction? firstAction = null;
        float? firstActionCost = null;
        
        foreach (var action in actionGroup.Actions)
        {
            float? costs = await GetCostsAsync(agv, action.TargetNodeId, cancellationToken);
            if (costs == null)
            {
                return null; // Break if a target node is not reachable
            }

            // keep first action and it's costs
            if (firstAction != null)
            {
                continue;
            }
            firstAction = action;
            firstActionCost = costs.GetValueOrDefault();
        }

        return firstActionCost;
    }

    /// <inheritdoc/>
    public async Task<bool> HasAnyAgvRouteAsync(Models.ActionGroup.ActionGroup actionGroup, IEnumerable<IAgv> agvs, CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        List<Task<float?>> hasRouteTasks = agvs.Select(agv => GetCostsForFirstActionAsync(agv, actionGroup, cts.Token)).ToList();

        while (hasRouteTasks.Any())
        {
            Task<float?> finishedTask = await Task.WhenAny(hasRouteTasks);

            if (finishedTask.Result != null)
            {
                _logger.LogDebug("A route calculation task completed successfully. Cancelling the remaining tasks.");
                cts.Cancel();
                return true;
            }

            _logger.LogDebug("A route calculation task completed but returned hasRoute false. Waiting for the next completion.");

            hasRouteTasks.Remove(finishedTask);
        }
        return false;
    }

    ///<inheritdoc />
    /// <exception cref="AgvLocationUnknownException"></exception>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    /// <exception cref="RouteNotFoundException"></exception>
    public async Task<IGraphAlgorithm.Route> GetRouteAsync(IAgv agv, string destinationNodeId, CancellationToken cancellationToken = default)
    {
        var route = await GetRouteIfExistingAsync(agv, destinationNodeId, cancellationToken);

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
        var route = await GetRouteIfExistingAsync(agv, destinationNodeIds);

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
                .Select(n => GetRouteIfExistingAsync(agv, n.Id)))
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