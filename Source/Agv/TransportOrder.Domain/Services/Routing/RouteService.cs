using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Routing.Base;
using Gebhardt.Acc.TransportOrder.Routing.Exceptions;
using Gebhardt.Acc.TransportOrder.Routing.Models;
using Microsoft.Extensions.Logging;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Routing;

/// <inheritdoc />
public class RouteService : IRouteService
{
    private readonly IGraphRepository _graphRepository;
    private readonly IRouteFinder _routeFinder;
    private readonly ILogger<RouteService> _logger;
    private readonly IQueueRepository _queueRepository;

    /// <inheritdoc cref="RouteService" />
    public RouteService(IGraphRepository graphRepository, IRouteFinder routeFinder, ILogger<RouteService> logger, IQueueRepository queueRepository)
    {
        _graphRepository = graphRepository;
        _routeFinder = routeFinder;
        _logger = logger;
        _queueRepository = queueRepository;
    }

    private static Guid GetLastNodeId(Agv agv)
    {
        return agv.AgvStatus?.LastNodeId ?? throw new AgvLocationUnknownException(agv.CorrelationId);
    }

    private async Task<IGraph> GetAgvGraph(Agv agv)
    {
        return await _graphRepository.GetAgvGraphAsync(agv.CorrelationId);
    }

    private async Task<Route?> GetRouteIfExistingAsync(Agv agv, Guid destinationNodeId, Guid? startNodeId = null, bool throwIfNodeUnknown = false, AgvData? agvData = null, CancellationToken cancellationToken = default)
    {
        if (startNodeId == null)
        {
            startNodeId = agv.AgvStatus?.LastNodeId;
            if (startNodeId == null)
            {
                _logger.LogWarning("Agv with CorrelationId {CorrelationId} has no last node set, cannot calculate route", agv.CorrelationId);
                return null;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!await _graphRepository.ExistsAsync(agv.CorrelationId))
        {
            _logger.LogWarning("AgvGraph for Agv with AgvId {AgvCorrelationId} does not exist, cannot calculate route", agv.CorrelationId);
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
                throw new NodeNotFoundException(agv, startNodeId.Value);
            }
        }

        var route = await _routeFinder.TryGetRouteAsync(agvGraph, agvData, cancellationToken, startNodeId.Value, destinationNodeId);

        return route?.Nodes.Length == 0 ? null : route;
    }

    private async Task<Route?> GetRouteIfExistingAsync(Agv agv, IEnumerable<Guid> destinationNodeIds, Guid? startNodeId, AgvData? agvData, CancellationToken cancellationToken = default)
    {
        startNodeId ??= GetLastNodeId(agv);

        var nodes = new List<Guid>();
        if (startNodeId != Guid.Empty)
        {
            nodes.Add(startNodeId.Value);
        }
        nodes.AddRange(destinationNodeIds);
        var agvGraph = await GetAgvGraph(agv);
        cancellationToken.ThrowIfCancellationRequested();

        var route = await _routeFinder.TryGetRouteAsync(agvGraph, agvData, cancellationToken, nodes.ToArray());
        return route;
    }

    ///<inheritdoc />
    /// <exception cref="AgvLocationUnknownException"></exception>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    public async Task<double?> GetCostsAsync(Agv agv, Guid destinationNodeId, AgvData? agvData, CancellationToken cancellationToken = default)
    {
        var route = await GetRouteIfExistingAsync(agv, destinationNodeId, null, true, agvData, cancellationToken);
        return route?.Costs;
    }

    /// <inheritdoc />
    public async Task<double?> GetCostsForFirstActionAsync(Agv agv, Models.ActionGroup.ActionGroup actionGroup, AgvData? agvData, CancellationToken cancellationToken = default)
    {
        AgvAction? firstAction = null;
        double? firstActionCost = null;

        if (actionGroup.Actions.All(a => a.IsMoveToCoordinate))
        {
            return 0;
        }

        foreach (var action in actionGroup.Actions.Where(a => a is { IsIncomplete: false, IsMoveToCoordinate: false }))
        {
            double? costs = await GetCostsAsync(agv, action.Target.Node!.Id, agvData, cancellationToken);
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

    /// <inheritdoc />
    public async Task<double?> GetCostsForFirstActionNotPassingNodesAsync(Agv agv, Models.ActionGroup.ActionGroup actionGroup, AgvData? agvData, ICollection<Guid> nodesToAvoid, CancellationToken cancellationToken = default)
    {
        AgvAction? firstAction = null;
        double? firstActionCost = null;

        if (actionGroup.Actions.All(a => a.IsMoveToCoordinate))
        {
            return 0;
        }

        foreach (var action in actionGroup.Actions.Where(a => a is { IsIncomplete: false, IsMoveToCoordinate: false }))
        {
            var route = await GetRouteIfExistingAsync(agv, action.Target.Node!.Id, null, true, agvData, cancellationToken);
            if (route == null)
            {
                return null; // Break if a target node is not reachable
            }

            IEnumerable<Guid> prohibitedNodes = route.Nodes.Where(n => n.Id != agv.AgvStatus?.LastNodeId).Select(n => n.Id).Intersect(nodesToAvoid);
            if (prohibitedNodes.Any() && action.ActionType == ActionType.Pick) // drop actions are not time-critical and can wait for prohibited nodes to be freed again
            {
                return null; // Break if a route to a pick passes nodes to avoid.
            }

            // keep first action and it's costs
            if (firstAction != null)
            {
                continue;
            }
            firstAction = action;
            firstActionCost = route.Costs;
        }

        return firstActionCost;
    }

    /// <inheritdoc/>
    public async Task<bool> HasAnyAgvRouteAsync(Models.ActionGroup.ActionGroup actionGroup, IEnumerable<Agv> agvs, CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        List<Task<double?>> hasRouteTasks = agvs.Select(agv =>
        {
            try
            {
                return GetCostsForFirstActionAsync(agv, actionGroup, null, cts.Token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while calculating route for Agv with CorrelationId {CorrelationId} in HasAnyAgvRouteAsync", agv.CorrelationId);
                return Task.FromResult<double?>(null);
            }
        }).ToList();

        while (hasRouteTasks.Count != 0)
        {
            Task<double?> finishedTask = await Task.WhenAny(hasRouteTasks);

            if (finishedTask.Result != null)
            {
                _logger.LogDebug("A route calculation task completed successfully. Cancelling the remaining tasks");
                await cts.CancelAsync();
                return true;
            }

            _logger.LogDebug("A route calculation task completed but returned hasRoute false. Waiting for the next completion");

            hasRouteTasks.Remove(finishedTask);
        }
        return false;
    }

    ///<inheritdoc />
    /// <exception cref="AgvLocationUnknownException"></exception>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    /// <exception cref="RouteNotFoundException"></exception>
    /// <exception cref="NodeNotFoundException"></exception>
    public async Task<Route> GetRouteAsync(Agv agv, Guid destinationNodeId, Guid? startNodeId = null, AgvData? agvData = null, CancellationToken cancellationToken = default)
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
    public async Task<Route> GetRouteAsync(Agv agv, IList<Guid> destinationNodeIds, Guid? startNodeId, AgvData? agvData)
    {
        var route = await GetRouteIfExistingAsync(agv, destinationNodeIds, startNodeId, agvData);

        if (route == null)
        {
            throw new RouteNotFoundException(agv.AgvStatus?.LastNodeId, destinationNodeIds.ToList());
        }

        return route;
    }

    ///<inheritdoc />
    public async Task<GraphNode> GetNodeWithLowestCostsAsync(Agv agv, IList<GraphNode> availableNodes, AgvData? agvData, Guid? startNodeId = null)
    {
        IEnumerable<GraphNode> graphNodes = availableNodes.ToList();
        List<Route> routes = await Task.WhenAll(graphNodes
                .Select(n => GetRouteIfExistingAsync(agv, n.Id, agvData: agvData, startNodeId: startNodeId)))
            .ContinueWith(x => x.Result.Where(route => route != null)
                .Cast<Route>().ToList(), TaskContinuationOptions.NotOnFaulted);

        if (routes.Count == 0)
        {
            throw new RouteNotFoundException(agv.CorrelationId, availableNodes);
        }
        var lowestCostDestinationNodeId = routes.MinBy(r => r.Costs)!.Nodes.Last().Id;

        _logger.LogDebug("Destination node with lowest cost route for Agv with serial number {SerialNumber} is: {Node}. This was chosen out of: {AvailableNodes}", agv.SerialNumber, lowestCostDestinationNodeId, graphNodes.Select(n => n.Name).ToList());
        return graphNodes.First(n => n.Id == lowestCostDestinationNodeId);
    }

    ///<inheritdoc />
    public async Task<(Guid? agv, Route? route)> GetAgvRouteClosestToTarget(ICollection<(Agv agv, AgvData? agvData)> agvs, Guid targetNodeId, bool skipAgvsStartingAtTarget = false)
    {
        if (skipAgvsStartingAtTarget)
        {
            // Skip agvs that are on the target or on the first edge to the next waypoint
            agvs = agvs.Where(a => a.agv.AgvStatus?.LastNodeId != targetNodeId).ToList();
        }

        List<(Guid CorrelationId, Route? Route)> routesWithIds = await Task.WhenAll(agvs.Select(async a =>
            {
                var route = await GetRouteIfExistingAsync(a.agv, targetNodeId, agvData: a.agvData, startNodeId: a.agv.AgvStatus?.LastNodeId);
                return (a.agv.CorrelationId, route);
            }))
            .ContinueWith(x => x.Result
                .Where(result => result.route != null)
                .Select(result => (result.CorrelationId, result.route))
                .ToList(), TaskContinuationOptions.NotOnFaulted);

        if (routesWithIds.Count == 0)
        {
            return (null, null);
        }

        ICollection<Guid> queueNodes = await _queueRepository.GetBlockedQueueNodes();

        List<(Guid CorrelationId, Route? Route)> routes = routesWithIds.Where(r =>
            r.Route != null &&
            (queueNodes.Contains(r.Route.Nodes.First().Id) || !r.Route.Nodes.Select(n => n.Id).Intersect(queueNodes).Any())).ToList();

        if (routes.Count == 0)
        {
            return (null, null);
        }
        return routes.MinBy(r => r.Route?.Costs);
    }
}