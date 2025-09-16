using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;
using AgvStatus = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.AgvStatus;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;

/// <inheritdoc cref="IMetricCollectionService"/>>
public class PrometheusMetricCollectionService(IAgvDb agvDb, IActionGroupRepository actionGroupRepository) : IMetricCollectionService
{
    /// <inheritdoc cref="IMetricCollectionService"/>>
    public async Task CollectOrderCompletionMetrics(Models.ActionGroup.ActionGroup actionGroup, Guid? lastNodeId = null)
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
        IEnumerable<AgvAction> actions = actionGroup.Actions.Where(a => a.ActionType == ActionType.Drop);
        double durationSeconds = actions
            .Where(action => action.Duration != null)
            .Sum(action => action.Duration!.Value.TotalSeconds);
        PrometheusMetricCollector.AgvStateBusyCargoGauge.WithLabels(agvCorrelationId.ToString()).Set(durationSeconds);
    }

    /// <summary>
    /// Collect the duration when agv was busy moving.
    /// </summary>
    /// <param name="actionGroup">The completed action group.</param>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    private void CollectBusyDuration(Models.ActionGroup.ActionGroup actionGroup, Guid agvCorrelationId)
    {
        IEnumerable<AgvAction> moveActions = actionGroup.Actions.Where(a => a.ActionType == ActionType.Move || a.ActionType == ActionType.Pick);
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
        IReadOnlyList<Models.ActionGroup.ActionGroup> latestActionGroups = await actionGroupRepository.GetActionGroupsAsync(states: [ActionGroupState.Completed, ActionGroupState.Aborted], toDate: currentActionGroup.CompletedDate!.Value.AddMilliseconds(-1), assignedAgvCorrelationId: agvCorrelationId, maxItems: 2, sortBy: SortBy.CompletedDate, sortDescending: true);
        if (latestActionGroups.Count < 1)
        {
            return;
        }
        var priorActionGroup = latestActionGroups.FirstOrDefault(a => a.CorrelationId != currentActionGroup.CorrelationId);
        if (priorActionGroup == null)
        {
            return;
        }
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
    public async Task CollectTravelDistance(Models.ActionGroup.ActionGroup actionGroup, Guid agvCorrelationId, Guid? lastNodeId)
    {
        var graph = await agvDb.Graphs.GetAgvGraphAsync(agvCorrelationId);
        double travelDistance = actionGroup.Actions.Sum(action =>
        {
            GraphNode[] route = action.Nodes;
            if (lastNodeId == null || route.All(n => n.Id != lastNodeId))
            {
                return graph.GetRouteLength(route);
            }
            int nodeIndex = Array.FindIndex(route, node => node.Id == lastNodeId);
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

    /// <inheritdoc cref="IMetricCollectionService"/>>
    public void CollectSafetyFieldViolationMetrics(Guid agvCorrelationId, List<AgvError> newErrors, List<AgvError> resolvedErrors)
    {
        foreach (var _ in newErrors.Where(e => e.IsFieldViolation()))
        {
            PrometheusMetricCollector.AgvSafetyFieldViolationCounter.WithLabels(agvCorrelationId.ToString()).Inc();
        }

        var now = DateTime.UtcNow;
        foreach (var violation in resolvedErrors.Where(e => e.IsFieldViolation()))
        {
            double timePassed = (now - violation.ErrorReceived).TotalSeconds;
            PrometheusMetricCollector.AgvSafetyFieldDurationGauge.WithLabels(agvCorrelationId.ToString()).Set(timePassed);
        }
    }

    /// <inheritdoc cref="IMetricCollectionService"/>>
    public void CollectJamStateMetrics(Guid stationId, DateTime lastChangedDate, bool previousHasPalletJam, DateTime currentChangedDate, bool currentHasPalletJam)
    {
        if (!previousHasPalletJam && currentHasPalletJam)
        {
            PrometheusMetricCollector.StationJamStateCounter.WithLabels(stationId.ToString()).Inc();
        }

        if (!currentHasPalletJam && previousHasPalletJam)
        {
            double timePassed = (currentChangedDate - lastChangedDate).TotalSeconds;
            PrometheusMetricCollector.StationJamStateDurationGauge.WithLabels(stationId.ToString()).Set(timePassed);
        }
    }
}