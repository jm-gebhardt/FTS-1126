using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Base;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Models;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;

/// <inheritdoc />
public class RouteService : IRouteService
{
    private readonly IAgvGraphRepository _agvGraphRepository;
    private readonly IRouteFinder _routeFinder;
    private readonly ILogger<RouteService> _logger;

    /// <inheritdoc cref="RouteService" />
    /// <param name="agvGraphRepository"></param>
    /// <param name="routeFinder"></param>
    /// <param name="logger"></param>
    public RouteService(IAgvGraphRepository agvGraphRepository, IRouteFinder routeFinder, ILogger<RouteService> logger)
    {
        _agvGraphRepository = agvGraphRepository;
        _routeFinder = routeFinder;
        _logger = logger;
    }

    private static string GetLastNodeId(Models.Agv.Agv agv)
    {
        return agv.AgvStatus?.LastNodeId ?? throw new AgvLocationUnknownException(agv.CorrelationId);
    }

    private async Task<AgvGraph> GetAgvGraph(Models.Agv.Agv agv)
    {
        return await _agvGraphRepository.GetAgvGraphAsync(agv.CorrelationId);
    }

    private async Task<Route?> GetRouteIfExistingAsync(Models.Agv.Agv agv, string destinationNodeId, string? startNodeId = null, bool throwIfNodeUnknown = false, CancellationToken cancellationToken = default)
    {
        startNodeId ??= GetLastNodeId(agv);

        cancellationToken.ThrowIfCancellationRequested();
        if (!await _agvGraphRepository.ExistsAsync(agv.CorrelationId))
        {
            _logger.LogWarning("AgvGraph for Agv with AgvId {AgvCorrelationId} does not exist. Cannot calculate route.", agv.CorrelationId);
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var agvGraph = await GetAgvGraph(agv);
        cancellationToken.ThrowIfCancellationRequested();

        if (throwIfNodeUnknown)
        {
            if (agvGraph.Nodes.All(n => n.Id != destinationNodeId))
            {
                throw new NodeNotFoundException(agv, destinationNodeId);
            }
            if (agvGraph.Nodes.All(n => n.Id != startNodeId))
            {
                throw new NodeNotFoundException(agv, startNodeId);
            }
        }

        var route = await _routeFinder.TryGetRouteAsync(agvGraph, cancellationToken, startNodeId, destinationNodeId);
        return route;
    }

    private async Task<Route?> GetRouteIfExistingAsync(Models.Agv.Agv agv, IEnumerable<string> destinationNodeIds, CancellationToken cancellationToken = default)
    {
        string lastNodeId = GetLastNodeId(agv);

        var nodes = new List<string>
        {
            lastNodeId
        };
        nodes.AddRange(destinationNodeIds);
        var agvGraph = await GetAgvGraph(agv);
        cancellationToken.ThrowIfCancellationRequested();

        var route = await _routeFinder.TryGetRouteAsync(agvGraph, cancellationToken, nodes.ToArray());
        return route;
    }

    ///<inheritdoc />
    /// <exception cref="AgvLocationUnknownException"></exception>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    public async Task<float?> GetCostsAsync(Models.Agv.Agv agv, string destinationNodeId, CancellationToken cancellationToken = default)
    {
        var route = await GetRouteIfExistingAsync(agv, destinationNodeId, null, true, cancellationToken);
        return route?.Costs;
    }

    /// <inheritdoc />
    public async Task<float?> GetCostsForFirstActionAsync(Models.Agv.Agv agv, Models.ActionGroup.ActionGroup actionGroup, CancellationToken cancellationToken = default)
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
    public async Task<bool> HasAnyAgvRouteAsync(Models.ActionGroup.ActionGroup actionGroup, IEnumerable<Models.Agv.Agv> agvs, CancellationToken cancellationToken = default)
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
    /// <exception cref="NodeNotFoundException"></exception>
    public async Task<Route> GetRouteAsync(Models.Agv.Agv agv, string destinationNodeId, string? startNodeId = null, CancellationToken cancellationToken = default)
    {
        var route = await GetRouteIfExistingAsync(agv, destinationNodeId, startNodeId, true, cancellationToken);

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
    public async Task<Route> GetRouteAsync(Models.Agv.Agv agv, IList<string> destinationNodeIds)
    {
        var route = await GetRouteIfExistingAsync(agv, destinationNodeIds);

        if (route == null)
        {
            throw new RouteNotFoundException(agv.AgvStatus?.LastNodeId, destinationNodeIds);
        }

        return route;
    }

    ///<inheritdoc />
    public async Task<GraphNode> GetNodeWithLowestCostsAsync(Models.Agv.Agv agv, IList<GraphNode> availableNodes)
    {
        IEnumerable<GraphNode> graphNodes = availableNodes.ToList();
        List<Route> routes = await Task.WhenAll(graphNodes
                .Select(n => GetRouteIfExistingAsync(agv, n.Id)))
            .ContinueWith(x => x.Result.Where(route => route != null)
                .Cast<Route>().ToList(), TaskContinuationOptions.NotOnFaulted);

        if (!routes.Any())
        {
            throw new RouteNotFoundException(agv.CorrelationId, availableNodes);
        }
        string lowestCostDestinationNodeId = routes.MinBy(r => r.Costs)!.Nodes.Last();

        _logger.LogDebug("Destination node with lowest cost route for Agv with AgvId {AgvId} is: {Node}. This was chosen out of: {AvailableNodes}", agv.AgvId, lowestCostDestinationNodeId, graphNodes.Select(n => n.Id).ToList());
        return graphNodes.First(n => n.Id == lowestCostDestinationNodeId);
    }
}