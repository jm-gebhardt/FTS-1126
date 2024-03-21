using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Metrics;

/// <summary>
/// Provides methods to collect metrics.
/// </summary>
public interface IMetricCollectionService
{
    /// <summary>
    /// Collect metrics on order completion
    /// </summary>
    /// <param name="actionGroup"></param>
    void CollectOrderCompletionMetrics(Models.ActionGroup.ActionGroup actionGroup);

    /// <summary>
    /// Collect agv battery charge metrics.
    /// </summary>
    /// <param name="agvStatus"></param>
    void CollectBatteryChargeMetrics(AgvStatus? agvStatus);
}