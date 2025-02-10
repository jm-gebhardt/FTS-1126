
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Provides methods to estimate action group duration estimates.
/// </summary>
public interface IActionGroupDurationEstimationService
{
    /// <summary>
    /// Get the duration from the distance between the positions based on  the maximum agv speed.
    /// </summary>
    /// <param name="agvVelocity">Maximum velocity of the agv in straight direction.</param>
    /// <param name="action">The action.</param>
    /// <param name="agvCorrelationId">The correlationId of the agv.</param>
    /// <returns>Duration in seconds.</returns>
    Task<double> GetDurationEstimate(double agvVelocity, AgvAction action, Guid agvCorrelationId);

    /// <summary>
    /// Add all costs that are in addition to the pure route costs:
    /// action switching, charging, cargo (un)loading
    /// </summary>
    /// <param name="lastActionWasMoveToCoordinate">True, if the last action was a move to a coordinate.</param>
    /// <param name="actionGroup">The action group.</param>
    /// <param name="action">The current action.</param>
    /// <param name="assignedAgv">The assigned agv</param>
    /// <returns>All added duration costs in seconds.</returns>
    Task<double> AddNonRouteCosts(bool lastActionWasMoveToCoordinate, Models.ActionGroup.ActionGroup actionGroup, AgvAction action, Agv assignedAgv);
}