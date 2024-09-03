using Gebhardt.Acc.Common.Domain.Models;
using AgvStatus = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.AgvStatus;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;

/// <summary>
/// Provides methods to collect metrics.
/// </summary>
public interface IMetricCollectionService
{
    /// <summary>
    /// Collect metrics on order completion.
    /// </summary>
    /// <param name="actionGroup">The action group.</param>
    /// <param name="lastNodeId">The last node id of the agv.</param>
    Task CollectOrderCompletionMetrics(Models.ActionGroup.ActionGroup actionGroup, string? lastNodeId=null);

    /// <summary>
    /// Collect agv battery charge metrics.
    /// </summary>
    /// <param name="agvStatus"></param>
    void CollectBatteryChargeMetrics(AgvStatus? agvStatus);

    /// <summary>
    /// Collect agv safety field violation metrics
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="newErrors">The list of new errors.</param>
    /// <param name="resolvedErrors">The list of resolved errors.</param>
    void CollectSafetyFieldViolationMetrics(Guid agvCorrelationId, List<AgvError> newErrors, List<AgvError> resolvedErrors);
}