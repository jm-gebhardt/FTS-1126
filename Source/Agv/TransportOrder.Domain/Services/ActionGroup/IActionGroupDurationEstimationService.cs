
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Provides methods to estimate action group duration estimates.
/// </summary>
public interface IActionGroupDurationEstimationService
{
    /// <summary>
    ///  Gets the average deviation between the actual and estimated
    /// duration [s] of an action group for the given target nodes.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="targetNodes">The target nodes.</param>
    /// <returns>The deviation in seconds.</returns>
    Task<double> GetAverageDurationEstimateDeviationAsync(Guid agvCorrelationId, string[] targetNodes);

    /// <summary>
    /// Get the duration from the distance between the positions based on  the maximum agv speed.
    /// </summary>
    /// <param name="agvVelocity">Maximum velocity of the agv in straight direction.</param>
    /// <param name="action">The action.</param>
    /// <param name="agvCorrelationId">The correlationId of the agv.</param>
    /// <returns>Duration in seconds.</returns>
    Task<double> GetDurationEstimate(double agvVelocity, AgvAction action, Guid agvCorrelationId);
}