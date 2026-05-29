using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.TrafficManagement;

/// <summary>
/// Manages AGV route dispatching.
/// </summary>
[PublicAPI]
public interface ITrafficManager
{
    /// <summary>
    /// Builds and dispatch a route for a new action.
    /// </summary>
    Task AddNewRouteAsync(Guid agvCorrelationId, string serialNumber, AgvAction action, CancellationToken ct = default);

    /// <summary>
    /// Builds and dispatch an updated route for a running action.
    /// </summary>
    Task UpdateRouteAsync(Guid agvCorrelationId, string serialNumber, AgvAction action, CancellationToken ct = default);

    /// <summary>
    /// Aborts the current route and dispatch a new one.
    /// </summary>
    Task AbortAndDispatchRouteAsync(Guid agvCorrelationId, string serialNumber, AgvAction action, Guid? abortedActionCorrelationId, CancellationToken ct = default);

    /// <summary>
    /// Resends the route for an existing action.
    /// </summary>
    Task ResendRouteAsync(Guid agvCorrelationId, string serialNumber, AgvAction action, CancellationToken ct = default);

    /// <summary>
    /// Handles a new-base request from an AGV.
    /// </summary>
    Task HandleNewBaseRequestAsync(Guid agvCorrelationId, CancellationToken ct = default);
}