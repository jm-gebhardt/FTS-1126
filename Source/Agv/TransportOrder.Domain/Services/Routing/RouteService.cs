using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
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
    private readonly IGraphRepository _graphRepository;
    private readonly IRouteFinder _routeFinder;
    private readonly ILogger<RouteService> _logger;

    /// <inheritdoc cref="RouteService" />
    /// <param name="graphRepository"></param>
    /// <param name="routeFinder"></param>
    /// <param name="logger"></param>
    public RouteService(IGraphRepository graphRepository, IRouteFinder routeFinder, ILogger<RouteService> logger)
    {
        _graphRepository = graphRepository;
        _routeFinder = routeFinder;
        _logger = logger;
    }

    private static string GetLastNodeId(Models.Agv.Agv agv)
    {
        return agv.AgvStatus?.LastNodeId ?? throw new AgvLocationUnknownException(agv.CorrelationId);
    }

    private async Task<IGraph> GetAgvGraph(Models.Agv.Agv agv)
    {
        return await _graphRepository.GetAgvGraphAsync(agv.CorrelationId);
    }

    private async Task<Route?> GetRouteIfExistingAsync(Models.Agv.Agv agv, string destinationNodeId, string? startNodeId = null, bool throwIfNodeUnknown = false, AgvData? agvData = null, CancellationToken cancellationToken = default)
    {
        startNodeId ??= GetLastNodeId(agv);

        cancellationToken.ThrowIfCancellationRequested();
        if (!await _graphRepository.ExistsAsync(agv.CorrelationId))
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

        var route = await _routeFinder.TryGetRouteAsync(agvGraph, agvData, cancellationToken, startNodeId, destinationNodeId);
        return route;
    }

    private async Task<Route?> GetRouteIfExistingAsync(Models.Agv.Agv agv, IEnumerable<string> destinationNodeIds, string? startNodeId, AgvData? agvData, CancellationToken cancellationToken = default)
    {
        startNodeId ??= GetLastNodeId(agv);

        var nodes = new List<string>
        {
            startNodeId
        };
        nodes.AddRange(destinationNodeIds);
        var agvGraph = await GetAgvGraph(agv);
        cancellationToken.ThrowIfCancellationRequested();

        var route = await _routeFinder.TryGetRouteAsync(agvGraph, agvData, cancellationToken, nodes.ToArray());
        return route;
    }

    ///<inheritdoc />
    /// <exception cref="AgvLocationUnknownException"></exception>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    public async Task<double?> GetCostsAsync(Models.Agv.Agv agv, string destinationNodeId, AgvData? agvData, CancellationToken cancellationToken = default)
    {
        var route = await GetRouteIfExistingAsync(agv, destinationNodeId, null, true, agvData, cancellationToken);
        return route?.Costs;
    }

    /// <inheritdoc />
    public async Task<double?> GetCostsForFirstActionAsync(Models.Agv.Agv agv, Models.ActionGroup.ActionGroup actionGroup, AgvData? agvData, CancellationToken cancellationToken = default)
    {
        AgvAction? firstAction = null;
        double? firstActionCost = null;

        foreach (var action in actionGroup.Actions)
        {
            double? costs = await GetCostsAsync(agv, action.TargetNodeId, agvData, cancellationToken);
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

        List<Task<double?>> hasRouteTasks = agvs.Select(agv => GetCostsForFirstActionAsync(agv, actionGroup, null, cts.Token)).ToList();

        while (hasRouteTasks.Any())
        {
            Task<double?> finishedTask = await Task.WhenAny(hasRouteTasks);

            if (finishedTask.Result != null)
            {
                _logger.LogDebug("A route calculation task completed successfully. Cancelling the remaining tasks.");
                await cts.CancelAsync();
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
    public async Task<Route> GetRouteAsync(Models.Agv.Agv agv, string destinationNodeId, string? startNodeId = null, AgvData? agvData = null, CancellationToken cancellationToken = default)
    {
        var route = await GetRouteIfExistingAsync(agv, destinationNodeId, startNodeId, true, agvData, cancellationToken);

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
    public async Task<Route> GetRouteAsync(Models.Agv.Agv agv, IList<string> destinationNodeIds, string? startNodeId, AgvData? agvData)
    {
        var route = await GetRouteIfExistingAsync(agv, destinationNodeIds, startNodeId, agvData);

        if (route == null)
        {
            throw new RouteNotFoundException(agv.AgvStatus?.LastNodeId, destinationNodeIds);
        }

        return route;
    }

    ///<inheritdoc />
    public async Task<GraphNode> GetNodeWithLowestCostsAsync(Models.Agv.Agv agv, IList<GraphNode> availableNodes, AgvData? agvData, string? startNodeId = null)
    {
        IEnumerable<GraphNode> graphNodes = availableNodes.ToList();
        List<Route> routes = await Task.WhenAll(graphNodes
                .Select(n => GetRouteIfExistingAsync(agv, n.Id, agvData: agvData, startNodeId: startNodeId)))
            .ContinueWith(x => x.Result.Where(route => route != null)
                .Cast<Route>().ToList(), TaskContinuationOptions.NotOnFaulted);

        if (!routes.Any())
        {
            throw new RouteNotFoundException(agv.CorrelationId, availableNodes);
        }
        string lowestCostDestinationNodeId = routes.MinBy(r => r.Costs)!.Nodes.Last();

        _logger.LogDebug("Destination node with lowest cost route for Agv with serial number {SerialNumber} is: {Node}. This was chosen out of: {AvailableNodes}", agv.SerialNumber, lowestCostDestinationNodeId, graphNodes.Select(n => n.Id).ToList());
        return graphNodes.First(n => n.Id == lowestCostDestinationNodeId);
    }
}