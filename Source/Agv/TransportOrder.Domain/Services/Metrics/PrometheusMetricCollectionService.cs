using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Metrics;

/// <inheritdoc cref="IMetricCollectionService"/>>
public class PrometheusMetricCollectionService(IAgvDb agvDb) : IMetricCollectionService
{
    /// <inheritdoc cref="IMetricCollectionService"/>>
    public async Task CollectOrderCompletionMetrics(Models.ActionGroup.ActionGroup actionGroup)
    {
        var agvCorrelationId = actionGroup.AssignedAgv!.Value;
        await CollectTravelDistance(actionGroup, agvCorrelationId);
        if (!actionGroup.IsMovingCargo || actionGroup.State == ActionGroupState.Aborted)
        {
            return;
        }
        PrometheusMetricCollector.AgvCompletedOrdersCounter.WithLabels(agvCorrelationId.ToString()).Inc();
    }

    private async Task CollectTravelDistance(Models.ActionGroup.ActionGroup actionGroup, Guid agvCorrelationId)
    {
        var graph = await agvDb.Graphs.GetAgvGraphAsync(agvCorrelationId);
        double travelDistance = actionGroup.Actions.Sum(action => graph.GetRouteLength(action.Nodes));
        PrometheusMetricCollector.AgvTravelDistanceGauge.WithLabels(agvCorrelationId.ToString()).Set(travelDistance);
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