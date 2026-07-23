using Gebhardt.Acc.Common.Messages.Route.RouteSteps;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Routes;

/// <summary>
/// Provides a route execution coordinator.
/// </summary>
public interface IRouteExecutionCoordinator
{
    /// <summary>
    /// Establish the planned step sequence for a route, all unreleased. Never sends anything to the AGV.
    /// </summary>
    /// <param name="routeId">The unique identifier of the route.</param>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="routeSteps">The planned steps, in traversal sequence.</param>
    Task<RouteCoordinationResult> PlanRouteAsync(Guid routeId, Guid agvCorrelationId, RouteStep[] routeSteps);

    /// <summary>
    /// Apply an updated step sequence to an existing route.
    /// </summary>
    /// <param name="routeId">The unique identifier of the route.</param>
    /// <param name="routeSteps">The updated steps, in traversal sequence.</param>
    Task<RouteCoordinationResult> UpdateRouteAsync(Guid routeId, RouteStep[] routeSteps);

    /// <summary>
    /// Release the named steps into the base, validating the base/horizon contiguity invariant, and mint an
    /// orderId on first release.
    /// </summary>
    /// <param name="routeId">The unique identifier of the route.</param>
    /// <param name="routeStepIds">The identifiers of the steps to release.</param>
    Task<RouteCoordinationResult> ReleaseRouteStepsAsync(Guid routeId, Guid[] routeStepIds);

    /// <summary>
    /// Abort a route. If an order is active, its cancellation is reported back via the result.
    /// </summary>
    /// <param name="routeId">The unique identifier of the route.</param>
    Task<RouteCoordinationResult> AbortRouteAsync(Guid routeId);

    /// <summary>
    /// Determine per-step progress.
    /// </summary>
    /// <param name="orderId">The orderId the state report is for.</param>
    /// <param name="orderState">The reported order state.</param>
    /// <returns>The changed step states, or null if the orderId is not owned by a known route, or if nothing changed.</returns>
    Task<RouteStepProgress?> DetermineRouteStepProgress(Guid orderId, OrderState orderState);
}
