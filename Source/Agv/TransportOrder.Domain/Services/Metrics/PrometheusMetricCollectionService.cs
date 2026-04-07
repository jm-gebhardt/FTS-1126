using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using JetBrains.Annotations;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;
using AgvStatus = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.AgvStatus;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;

/// <inheritdoc cref="IMetricCollectionService"/>>
[PublicAPI]
public class PrometheusMetricCollectionService(IAgvDb agvDb, IActionGroupRepository actionGroupRepository, IStationRepository stationRepository) : IMetricCollectionService
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
        PrometheusMetricCollector.AgvStateBusyCargoCounter.WithLabels(agvCorrelationId.ToString()).Inc(durationSeconds);
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
        PrometheusMetricCollector.AgvStateBusyCounter.WithLabels(agvCorrelationId.ToString()).Inc(durationSeconds);
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
        IReadOnlyList<Models.ActionGroup.ActionGroup> latestActionGroups = await actionGroupRepository.GetActionGroupsAsync(
            states: [ActionGroupState.Completed, ActionGroupState.Aborted],
            toDate: currentActionGroup.CompletedDate?.AddMilliseconds(-1),
            assignedAgvCorrelationId: agvCorrelationId,
            maxItems: 2, 
            sortBy: SortBy.CompletedDate, 
            sortDescending: true);
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
        PrometheusMetricCollector.AgvStateIdleCounter.WithLabels(agvCorrelationId.ToString()).Inc(durationSeconds);
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
        PrometheusMetricCollector.AgvStateChargingCounter.WithLabels(agvCorrelationId.ToString()).Inc(chargeAction.Duration.Value.TotalSeconds);
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
        double travelDistance = actionGroup.Actions.Sum(action => graph.GetRouteLength(action.Edges, lastNodeId));
        PrometheusMetricCollector.AgvTravelDistanceCounter.WithLabels(agvCorrelationId.ToString()).Inc(travelDistance);
    }

    /// <inheritdoc cref="IMetricCollectionService"/>>
    public void CollectBatteryChargeMetrics(AgvStatus? agvStatus)
    {
        if (agvStatus == null)
        {
            return;
        }
        string agvId = agvStatus.CorrelationId.ToString();
        PrometheusMetricCollector.BatteryChargeGauge.WithLabels(agvId).Set(agvStatus.BatteryCharge);
        // Ensure offline counter label exists at 0 so Prometheus has a baseline before any offline event.
        // Without this, increase() can't detect the first offline→online transition.
        PrometheusMetricCollector.AgvStateOfflineCounter.WithLabels(agvId);
    }

    /// <inheritdoc cref="IMetricCollectionService"/>>
    public void CollectOfflineDuration(Guid agvCorrelationId, bool isNowOnline)
    {
        if (!isNowOnline)
        {
            PrometheusMetricCollector.AgvOfflineSince[agvCorrelationId] = DateTime.UtcNow;
            return;
        }

        if (PrometheusMetricCollector.AgvOfflineSince.TryRemove(agvCorrelationId, out var offlineSince))
        {
            double duration = (DateTime.UtcNow - offlineSince).TotalSeconds;
            PrometheusMetricCollector.AgvStateOfflineCounter.WithLabels(agvCorrelationId.ToString()).Inc(duration);
        }
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

    /// <inheritdoc cref="IMetricCollectionService"/>>
    public async Task CollectStationOrderCompletionMetrics(ActionType actionType, Guid targetNodeId)
    {
        if (!new List<ActionType> { ActionType.Charge, ActionType.Pick, ActionType.Drop }.Contains(actionType))
        {
            return;
        }
        var station = await stationRepository.GetRequiredStationBaseInformation(targetNodeId);
        if (station == null)
        {
            return;
        }

        switch (actionType)
        {
            case ActionType.Charge:
                PrometheusMetricCollector.StationChargeCounter.WithLabels(station.Id.ToString()).Inc();
                break;

            case ActionType.Pick:
                PrometheusMetricCollector.StationPickCounter.WithLabels(station.Id.ToString()).Inc();
                break;

            case ActionType.Drop:
                PrometheusMetricCollector.StationDropCounter.WithLabels(station.Id.ToString()).Inc();
                break;

            case ActionType.Move:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null);
        }
    }
}