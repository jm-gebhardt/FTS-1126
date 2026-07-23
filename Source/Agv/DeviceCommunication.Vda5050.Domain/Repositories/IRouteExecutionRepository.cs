using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Routes;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;

/// <summary>
/// Provides a repository for <see cref="RouteExecution"/> related queries.
/// </summary>
public interface IRouteExecutionRepository
{
    /// <summary>
    /// Get a route execution by its route id, if available.
    /// </summary>
    /// <param name="routeId">The route id.</param>
    /// <returns>The route execution, or null if none found.</returns>
    Task<RouteExecution?> GetAsync(Guid routeId);

    /// <summary>
    /// Get a route execution by its route id.
    /// </summary>
    /// <param name="routeId">The route id.</param>
    /// <returns>The route execution.</returns>
    /// <exception cref="RouteExecutionNotFoundException"></exception>
    Task<RouteExecution> GetRequiredAsync(Guid routeId);

    /// <summary>
    /// Get the currently active route execution for the given agv, if any.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <returns>The active route execution, or null if the agv has none.</returns>
    Task<RouteExecution?> GetActiveByAgvCorrelationIdAsync(Guid agvCorrelationId);

    /// <summary>
    /// Get the route execution currently owning the given VDA5050 orderId, if any.
    /// </summary>
    /// <param name="orderId">The current orderId.</param>
    /// <returns>The owning route execution, or null if none found.</returns>
    Task<RouteExecution?> GetByCurrentOrderIdAsync(Guid orderId);

    /// <summary>
    /// Add a new route execution.
    /// </summary>
    /// <param name="routeExecution">The route execution.</param>
    Task AddAsync(RouteExecution routeExecution);

    /// <summary>
    /// Persist changes made to an existing route execution.
    /// </summary>
    /// <param name="routeExecution">The route execution.</param>
    Task UpdateAsync(RouteExecution routeExecution);
}
