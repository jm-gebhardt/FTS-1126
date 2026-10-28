using Gebhardt.Acc.Common.Messages.Route;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Reservations;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Generation;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Manager;

using CommonRouteStepStatus = Common.Messages.Route.RouteSteps.RouteStepStatus;

/// <summary>
/// The RouteManager is responsible for managing the routes for AGVs. It handles route invocation, execution, and completion. It interacts with the RouteGenerator
/// to create routes and the ReservationManager to manage reservations for route steps.
/// </summary>
internal class RouteManager(RouteGenerator routeGenerator, ReservationManager reservationManager, IBus bus, ILogger<RouteManager> logger)
{
    private readonly Dictionary<Guid, Route> _routes = [];

    /// <summary>
    /// Collection of all active routes.
    /// </summary>
    public IReadOnlyCollection<Route> Routes => _routes.Values;

    public void InvokeRoute(RouteRequest request)
    {
        var agv = request.Agv;

        if (!agv.IsAutomatic)
        {
            logger.LogError("Route for AGV {AgvCorrelationId} cannot be invoked because the AGV is not in automatic mode", agv.Id);
            bus.Publish(new ActionFailedByAgv(request.ActionCorrelationId, agv.Id));
            return;
        }

        if (_routes.ContainsKey(agv.Id))
        {
            logger.LogError("Route for AGV {AgvCorrelationId} already exists", agv.Id);
            bus.Publish(new ActionFailedByAgv(request.ActionCorrelationId, agv.Id));
            return;
        }

        var startNode = agv.CurrentRoutingNode;

        if (startNode is null)
        {
            logger.LogWarning("AGV {AgvCorrelationId} has no valid start node", agv.Id);
            bus.Publish(new ActionFailedByAgv(request.ActionCorrelationId, agv.Id));
            return;
        }

        var creationRequest = new RouteCreationRequest(agv, startNode, request.TargetNodeCandidates, request.ActionCorrelationId, request.WhitelistedActionEdges);

        var route = routeGenerator.CreateRoute(creationRequest);

        if (route is null)
        {
            logger.LogError("No route found for AGV {AgvCorrelationId}", agv.Id);
            bus.Publish(new ActionFailedByAgv(request.ActionCorrelationId, agv.Id));
            return;
        }

        logger.LogInformation("Route created for AGV {AgvCorrelationId} \n\n{Route}\n", agv.Id, string.Join("\n", route.Steps.Select(s => s.ToString())));

        _routes[agv.Id] = route;

        bus.Publish(new RoutePlanned(route.Id, route.Agv.Id, [.. route.Steps.Select(s => s.ToMessage())]));
        bus.Publish(new ActionStartedByAgv(route.ActionCorrelationId, agv.Id));

        ExecuteRoute(route);
    }

    public bool AbortRoute(Guid agvId, Guid actionCorrelationId)
    {
        logger.LogInformation("Aborting route for AGV {AgvCorrelationId}", agvId);

        if (!_routes.TryGetValue(agvId, out var route))
        {
            logger.LogWarning("Route of AGV {AgvCorrelationId} not found", agvId);
            bus.Publish(new ActionAbortionFailedByAgv(actionCorrelationId, agvId));
            return false;
        }

        foreach (var routeStep in route.Steps.Where(s => !s.IsCompleted))
        {
            UnreserveRouteStep(routeStep);
            routeStep.SetFailed();
        }

        _routes.Remove(agvId);

        bus.Publish(new RouteAborted(route.Id, route.Agv.Id));
        bus.Publish(new ActionAbortedByAgv(route.ActionCorrelationId, route.Agv.Id));

        return true;
    }

    public Route? GetRoute(Guid agvId)
    {
        return _routes.GetValueOrDefault(agvId);
    }

    public void ExecuteRoutes()
    {
        foreach (var route in Routes.ToList())
        {
            ExecuteRoute(route);
        }
    }

    private void ExecuteRoute(Route route)
    {
        var agv = route.Agv;

        if (!agv.IsAutomatic)
        {
            return;
        }

        if (route.Steps.All(s => s.IsCompleted))
        {
            logger.LogInformation("Route for AGV {AgvCorrelationId} is completed (all steps completed)", route.Agv.Id);
            CompleteRoute(route);
            return;
        }

        double reservationLength = route.Steps.Where(s => s.Status is RouteStepStatus.Released or RouteStepStatus.Acknowledged).Sum(s => s.Length);

        var reservedSteps = new List<RouteStep>();

        while (reservationLength < TrafficManagementConfig.MaxReservationLength)
        {
            var nextStep = route.Steps.FirstOrDefault(s => s.IsPlanned);

            if (nextStep is null)
            {
                break;
            }

            if (!TryReserveStep(nextStep, route.Agv.Id))
            {
                break;
            }

            nextStep.SetReleased();
            reservedSteps.Add(nextStep);

            reservationLength += nextStep.Length;
        }

        bus.Publish(new RouteStepsReleased(route.Id, route.Agv.Id, [.. reservedSteps.Select(s => s.RouteStepId)]));
    }

    public void ProcessRouteSteps(RouteStepsProcessed routeStepsProcessed)
    {
        if (!_routes.TryGetValue(routeStepsProcessed.AgvCorrelationId, out var route))
        {
            logger.LogWarning("No route found for AGV {AgvCorrelationId}", routeStepsProcessed.AgvCorrelationId);
            return;
        }

        if (route.Id != routeStepsProcessed.RouteId)
        {
            logger.LogWarning("Route ID mismatch for AGV {AgvCorrelationId}. Expected {ExpectedRouteId}, but got {ActualRouteId}", routeStepsProcessed.AgvCorrelationId, route.Id, routeStepsProcessed.RouteId);
            return;
        }

        if (routeStepsProcessed.RouteStepStates.Length == 0)
        {
            logger.LogWarning("No route step states provided for AGV {AgvCorrelationId}", routeStepsProcessed.AgvCorrelationId);
            return;
        }

        var lastRouteStep = route.Steps.LastOrDefault();
        bool isAnyStepCompleted = false;
        bool isLastStepCompleted = false;

        foreach (var routeStepState in routeStepsProcessed.RouteStepStates)
        {
            var routeStep = route.Steps.FirstOrDefault(s => s.RouteStepId == routeStepState.RouteStepId);

            if (routeStep is null)
            {
                logger.LogWarning("No route step found for AGV {AgvCorrelationId} with RouteStepId {RouteStepId}", routeStepsProcessed.AgvCorrelationId, routeStepState.RouteStepId);
                continue;
            }

            switch (routeStepState.Status)
            {
                case CommonRouteStepStatus.Acknowledged:
                    routeStep.SetAcknowledged();
                    break;

                case CommonRouteStepStatus.Completed:

                    UnreserveRouteStep(routeStep);
                    routeStep.SetCompleted();
                    UpdateAgvPositionAfterRouteStepCompleted(routeStep, route);

                    isAnyStepCompleted = true;
                    if (routeStep == lastRouteStep) { isLastStepCompleted = true; }
                    break;

                case CommonRouteStepStatus.Failed:
                    routeStep.SetFailed();

                    bus.Publish(new ActionFailedByAgv(route.ActionCorrelationId, route.Agv.Id));
                    //todo: handle failed route step (e.g., abort route, forward failure reason).
                    AbortRoute(routeStepsProcessed.AgvCorrelationId, route.ActionCorrelationId);
                    return;
            }
        }

        if (isLastStepCompleted)
        {
            CompleteRoute(route);
        }
        else if (isAnyStepCompleted)
        {
            ExecuteRoute(route);
        }
    }

    private void UpdateAgvPositionAfterRouteStepCompleted(RouteStep routeStep, Route route)
    {
        var prevousSteps = route.Steps.TakeWhile(s => s != routeStep);
        Trace.Assert(prevousSteps.All(s => s.IsCompleted));

        var subsequentSteps = route.Steps.SkipWhile(s => s != routeStep).Skip(1);
        Trace.Assert(subsequentSteps.All(s => !s.IsCompleted));

        var agv = route.Agv;
        if (routeStep.RoutingEdge.EndNode != agv.CurrentRoutingNode)
        {
            agv.CurrentRoutingNode = routeStep.RoutingEdge.EndNode;
            logger.LogInformation("State updated: {AgvState}", agv);
        }
    }

    private bool TryReserveStep(RouteStep routeStep, Guid agvCorrelationId)
    {
        if (reservationManager.TryReserve([routeStep.RoutingEdge, routeStep.RoutingEdge.EndNode], agvCorrelationId, out var tickets))
        {
            routeStep.AddReservations(tickets);
            return true;
        }

        return false;
    }

    private void UnreserveRouteStep(RouteStep routeStep)
    {
        foreach (var reservation in routeStep.Reservations)
        {
            reservationManager.Unreserve(reservation);
        }

        routeStep.ClearReservations();
    }

    private void CompleteRoute(Route route)
    {
        Trace.Assert(route.Steps.All(s => s.IsCompleted));

        _routes.Remove(route.Agv.Id);

        bus.Publish(new ActionCompletedByAgv(route.ActionCorrelationId, route.Agv.Id));
    }
}