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
}