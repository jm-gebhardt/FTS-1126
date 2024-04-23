using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

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
}