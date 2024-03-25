using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using AgvAction = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Metrics;

/// <inheritdoc cref="IMetricCollectionService"/>>
public class PrometheusMetricCollectionService(IAgvDb agvDb, IActionGroupRepository actionGroupRepository) : IMetricCollectionService
{
    /// <inheritdoc cref="IMetricCollectionService"/>>
    public async Task CollectOrderCompletionMetrics(Models.ActionGroup.ActionGroup actionGroup, string? lastNodeId=null)
    {
        var agvCorrelationId = actionGroup.AssignedAgv!.Value;
        await CollectTravelDistance(actionGroup, agvCorrelationId, lastNodeId);
        CollectOrdersCount(actionGroup, agvCorrelationId);
        CollectBusyDuration(actionGroup, agvCorrelationId);
        CollectBusyCargoDuration(actionGroup, agvCorrelationId);
        await CollectIdleDuration(actionGroup, agvCorrelationId);
        CollectChargingDuration(actionGroup, agvCorrelationId);
    }

    private void CollectBusyCargoDuration(Models.ActionGroup.ActionGroup actionGroup, Guid agvCorrelationId)
    {
        if (!actionGroup.IsMovingCargo)
        {
            return;
        }
        IEnumerable<AgvAction> actions = actionGroup.Actions.Where(a => a.ActionType == ActionType.Pick || a.ActionType == ActionType.Drop);
        double durationSeconds = actions
            .Where(action => action.Duration != null)
            .Sum(action => action.Duration!.Value.TotalSeconds);
        PrometheusMetricCollector.AgvStateBusyCargoGauge.WithLabels(agvCorrelationId.ToString()).Set(durationSeconds);
    }

    /// <summary>
    /// Collect the duration when agv was busy moving w/o cargo.
    /// </summary>
    /// <param name="actionGroup">The completed action group.</param>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    private void CollectBusyDuration(Models.ActionGroup.ActionGroup actionGroup, Guid agvCorrelationId)
    {
        if (actionGroup.IsMovingCargo)
        {
            return;
        }
        IEnumerable<AgvAction> moveActions = actionGroup.Actions.Where(a => a.ActionType == ActionType.Move);
        double durationSeconds = moveActions
            .Where(moveAction => moveAction.Duration != null)
            .Sum(moveAction => moveAction.Duration!.Value.TotalSeconds);
        PrometheusMetricCollector.AgvStateBusyGauge.WithLabels(agvCorrelationId.ToString()).Set(durationSeconds);
    }

    /// <summary>
    /// Collect the duration of idle times for an agv.
    /// </summary>
    /// <param name="currentActionGroup">The current action group.</param>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    private async Task CollectIdleDuration(Models.ActionGroup.ActionGroup currentActionGroup, Guid agvCorrelationId)
    {
        // Accept delay in idle duration collection.
        // Collection during completion ensures the agv accepted and actually
        // executed the action group and dates are written to the db.
        IReadOnlyList<Models.ActionGroup.ActionGroup> latestActionGroups = await actionGroupRepository.GetActionGroupsAsync(states: [ActionGroupState.Completed, ActionGroupState.Aborted], toDate:currentActionGroup.CompletedDate!.Value.AddMilliseconds(-1), assignedAgvCorrelationId: agvCorrelationId, maxItems: 1, sortBy: SortBy.CompletedDate, sortDescending: true);
        if (latestActionGroups.Count < 1)
        {
            return;
        }
        var priorActionGroup = latestActionGroups.Single();
        TimeSpan? duration = currentActionGroup.StartedDate - priorActionGroup.CompletedDate;
        if (duration == null)
        {
            return;
        }
        
        double durationSeconds = duration.Value.TotalSeconds + GetIdleDurationInBetweenActions(currentActionGroup);
        PrometheusMetricCollector.AgvStateIdleGauge.WithLabels(agvCorrelationId.ToString()).Set(durationSeconds);
    }

    /// <summary>
    /// Collect the idle duration in between action groups.
    /// </summary>
    /// <param name="actionGroup">The completed action group</param>
    /// <returns>The idle time [s].</returns>
    private double GetIdleDurationInBetweenActions(Models.ActionGroup.ActionGroup actionGroup)
    {
        var duration = TimeSpan.Zero;
        List<AgvAction> actions = actionGroup.Actions.OrderBy(a => a.CompletedDate).ToList();
        for (int i = 0; i < actions.Count - 1; i++)
        {
            var currentAction = actions[i];
            var nextAction = actions[i + 1];

            if (!currentAction.CompletedDate.HasValue || !nextAction.StartedDate.HasValue)
            {
                continue;
            }
            var difference = nextAction.StartedDate.Value - currentAction.CompletedDate.Value;
            duration += difference;
        }

        return duration.TotalSeconds;
    }
    
    /// <summary>
    /// Collect the duration of charging.
    /// </summary>
    /// <param name="actionGroup">The charging action group.</param>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    private void CollectChargingDuration(Models.ActionGroup.ActionGroup actionGroup, Guid agvCorrelationId)
    {
        if (!actionGroup.IsChargingOrder)
        {
            return;
        }

        var chargeAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Charge);
        if (chargeAction.Duration == null)
        {
            return;
        }
        PrometheusMetricCollector.AgvStateChargingGauge.WithLabels(agvCorrelationId.ToString()).Set(chargeAction.Duration.Value.TotalSeconds);
    }

    private void CollectOrdersCount(Models.ActionGroup.ActionGroup actionGroup, Guid agvCorrelationId)
    {
        if (!actionGroup.IsMovingCargo || actionGroup.State == ActionGroupState.Aborted)
        {
            return;
        }
        PrometheusMetricCollector.AgvCompletedOrdersCounter.WithLabels(agvCorrelationId.ToString()).Inc();
    }

    /// <summary>
    /// Collect the distance traveled during execution of an action group.
    /// </summary>
    /// <param name="actionGroup">The action group.</param>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="lastNodeId">The last node id of the agv.</param>
    /// <returns></returns>
    public async Task CollectTravelDistance(Models.ActionGroup.ActionGroup actionGroup, Guid agvCorrelationId, string? lastNodeId)
    {
        var graph = await agvDb.Graphs.GetAgvGraphAsync(agvCorrelationId);
        double travelDistance = actionGroup.Actions.Sum(action =>
        {
            string[] route = action.Nodes;
            if (lastNodeId == null || !route.Contains(lastNodeId))
            {
                return graph.GetRouteLength(route);
            }
            int nodeIndex = Array.IndexOf(route, lastNodeId);
            route = route.Take(nodeIndex + 1).ToArray();
            return graph.GetRouteLength(route);
        });
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