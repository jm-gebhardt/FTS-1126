using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Metrics;

/// <inheritdoc cref="IMetricCollectionService"/>>
public class PrometheusMetricCollectionService : IMetricCollectionService
{
    /// <inheritdoc cref="IMetricCollectionService"/>>
    public void CollectOrderCompletionMetrics(Models.ActionGroup.ActionGroup actionGroup)
    {
        if (!actionGroup.IsMovingCargo)
        {
            return;
        }
        PrometheusMetricCollector.AgvCompletedOrdersCounter.WithLabels(actionGroup.AssignedAgv!.Value.ToString()).Inc();
    }

    /// <inheritdoc cref="IMetricCollectionService"/>>
    public void CollectBatteryChargeMetrics(AgvStatus? agvStatus)
    {
        if (agvStatus == null)
        {
            return;
        }
        PrometheusMetricCollector.BatteryChargeGauge.WithLabels(agvStatus.CorrelationId.ToString()).Set(agvStatus.BatteryCharge);
    }
}