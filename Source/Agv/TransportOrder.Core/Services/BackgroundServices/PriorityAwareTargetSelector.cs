using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;

/// <summary>
/// Handles Decision Point 1 (DP1) target selection logic with consideration of short loop order priority.
/// </summary>
public class PriorityAwareTargetSelector(
    ILogger<PriorityAwareTargetSelector> logger,
    IStationRepository stationRepository,
    IActionGroupRepository actionGroupRepository,
    ISemaphoreProvider<ITargetSelectionSemaphore> semaphoreProvider,
    IDecisionPointActionRepository decisionPointActionRepository,
    INodeStateRepository nodeStateRepository,
    IBus bus)
    : ITargetSelector
{
    private readonly ConcurrentDictionary<Guid, DateTime> _actionsToSelectTarget = new();
    private readonly List<Guid> _lastEvaluatedActions = [];
    private ICollection<Guid> _consideredStations = [];
    private bool _useJamState;

    /// <summary>
    /// Actions pending target selection.
    /// </summary>
    public IReadOnlyDictionary<Guid, DateTime> PendingActions => _actionsToSelectTarget;

    /// <summary>
    /// Evaluated actions.
    /// </summary>
    public IReadOnlyCollection<Guid> EvaluatedActions => _lastEvaluatedActions;

    /// <summary>
    /// True, if there are no actions pending target selection.
    /// </summary>
    public bool IsEmpty => _actionsToSelectTarget.IsEmpty;

    /// <summary>
    /// Process actions.
    /// </summary>
    /// <param name="skipForShortLoopOrder"> Callback for checking short loop order priority.</param>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns></returns>
    public async Task ProcessActions(Func<Guid, bool> skipForShortLoopOrder, CancellationToken stoppingToken)
    {
        if (_actionsToSelectTarget.IsEmpty)
        {
            logger.LogDebug("No long loop action listed for target selection");
            return;
        }
        await semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            Guid? action = GetNextActionToProcess();
            if (action == null)
            {
                logger.LogDebug("No long loop action found for target selection");
                return;
            }

            var actionGroup = await actionGroupRepository.GetRequiredActionGroupByActionIdAsync(action.Value);
            if (_lastEvaluatedActions.Contains(action.Value))
            {
                return;
            }

            var agvCorrelationId = actionGroup.AssignedAgv!.Value;
            logger.LogInformation("Trigger target selection for agv {Agv} running action group {ActionGroup}", agvCorrelationId, actionGroup.CorrelationId);

            Guid? stationId = await SelectTarget(_consideredStations, action.Value, skipForShortLoopOrder, agvCorrelationId);
            if (stationId != null)
            {
                await AssignTarget(action.Value, actionGroup, agvCorrelationId, stationId.Value, stoppingToken);
            }
        }, stoppingToken);
    }

    private Guid? GetNextActionToProcess()
    {
        if (_consideredStations.Count == 1 && !_actionsToSelectTarget.IsEmpty)
        {
            return _actionsToSelectTarget.MinBy(a => a.Value).Key;
        }

        if (_consideredStations.Count > 1)
        {
            var lastEvaluatedSet = new HashSet<Guid>(_lastEvaluatedActions);
            return _actionsToSelectTarget
                .Where(kvp => !lastEvaluatedSet.Contains(kvp.Key))
                .OrderBy(kvp => kvp.Value)
                .Select(kvp => (Guid?)kvp.Key)
                .FirstOrDefault();
        }

        return null;
    }

    private async Task<Guid?> SelectTarget(ICollection<Guid> consideredStations, Guid actionId, Func<Guid, bool> shouldSkipStation, Guid agvCorrelationId)
    {
        ICollection<Station> stations = await stationRepository.GetStationsById(consideredStations);

        ICollection<Station> availableStations = GetAvailableStations(stations);
        Guid? selectedTarget = null;
        foreach (var station in availableStations)
        {
            if (!station.IsEmpty)
            {
                logger.LogInformation("Station: {Station} is not free, as it's occupied by {AgvCorrelationId} agv, StationCount set to {StationCount}",
                    station.Id, station.NodeLock?.AgvCorrelationId, consideredStations);
                continue;
            }
            if (_useJamState)
            {
                logger.LogInformation("Considering jam state for station {Target}", station.Id);
                if (station.HasPalletJam)
                {
                    logger.LogInformation("Station {Target} not available due to pallet jam", station.Id);
                    continue;
                }
            }

            if (shouldSkipStation(station.Id))
            {
                logger.LogInformation("Skipping station {Station} for DP1 action {Action} as there is a short loop order that has priority", station.Id, actionId);
                continue;
            }
            if (!await nodeStateRepository.ReserveNodeAsync(station.Id, agvCorrelationId))
            {
                continue;
            }
            selectedTarget = station.Id;
            break;
        }

        return selectedTarget;
    }

    private async Task AssignTarget(Guid actionId, ActionGroup actionGroup, Guid agvCorrelationId, Guid stationId, CancellationToken stoppingToken)
    {
        logger.LogInformation("Selected target {Target} for agv {Agv} running action group {ActionGroup} and adding to station",
            stationId, agvCorrelationId, actionGroup.CorrelationId);
        var nodeState = await nodeStateRepository.GetNodeStateAsync(stationId);
        await bus.Publish(new NodeStateChanged(nodeState!, stationId, null, null, null), stoppingToken);
        _lastEvaluatedActions.Add(actionId);
        await bus.Publish(new SetTargetForAction(actionId, actionGroup.CorrelationId, agvCorrelationId, stationId), stoppingToken);

        var decisionPointAction = await decisionPointActionRepository.GetByIdAsync(actionId);
        decisionPointAction.SetTarget(stationId);
        await decisionPointActionRepository.UpdateAsync(decisionPointAction);
    }

    private ICollection<Station> GetAvailableStations(ICollection<Station> stations)
    {
        ICollection<Station> orderedOccupancies = [];
        foreach (var station in stations)
        {
            if (!station.IsLocked)
            {
                orderedOccupancies.Add(station);
            }
        }
        return orderedOccupancies;
    }

    /// <inheritdoc cref="ITargetSelector"/>>
    public void SetStationCount(ICollection<Guid> stations)
    {
        _consideredStations = stations;
    }

    /// <inheritdoc cref="ITargetSelector"/>>
    public void SetUseJamState(bool useJamState)
    {
        _useJamState = useJamState;
    }

    /// <inheritdoc cref="ITargetSelector"/>>
    public async Task Enqueue(Guid actionCorrelationId, Guid agvCorrelationId)
    {
        var now = DateTime.UtcNow;
        logger.LogInformation("Listing DP1 action {ActionCorrelationId} for target selection", actionCorrelationId);
        _actionsToSelectTarget.TryAdd(actionCorrelationId, now);
        var decisionPointAction = new DecisionPointAction(actionCorrelationId, agvCorrelationId, now, _consideredStations);
        await decisionPointActionRepository.AddAsync(decisionPointAction);
    }

    /// <inheritdoc cref="ITargetSelector"/>>
    public async Task Dequeue(Guid actionCorrelationId)
    {
        if (_actionsToSelectTarget.ContainsKey(actionCorrelationId))
        {
            await RemoveAction(actionCorrelationId);
            return;
        }

        logger.LogInformation("Trying to dequeue DP1 action {ActionCorrelationId}", actionCorrelationId);
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);
        var skippedAction = actionGroup.Actions.SingleOrDefault(a => a.ShouldSkip);

        if (skippedAction == null || !_actionsToSelectTarget.ContainsKey(skippedAction.CorrelationId))
        {
            logger.LogWarning("Could not find DP1 action {ActionCorrelationId} or skipped action on action group {ActionGroupCorrelationId}",
                actionCorrelationId, actionGroup.CorrelationId);
            return;
        }

        await RemoveAction(skippedAction.CorrelationId);
    }

    private async Task RemoveAction(Guid correlationId)
    {
        if (_actionsToSelectTarget.TryRemove(correlationId, out _))
        {
            _lastEvaluatedActions.Remove(correlationId);
            logger.LogInformation("Dequeued DP1 action {ActionCorrelationId}", correlationId);
            await decisionPointActionRepository.RemoveById(correlationId);
        }
    }

    /// <inheritdoc cref="ITargetSelector"/>>
    public async Task Initialize()
    {
        ICollection<DecisionPointAction> decisionPointActions = await decisionPointActionRepository.GetAll();
        if (decisionPointActions.Count > 0)
        {
            _consideredStations = decisionPointActions.MaxBy(a => a.EntryTime)?.ConsideredStations ?? [];
        }
        foreach (var action in decisionPointActions)
        {
            if (action.TargetNodeId == null)
            {
                _actionsToSelectTarget.TryAdd(action.AgvActionCorrelationId, action.EntryTime);
            }
            else
            {
                var actionGroup = await actionGroupRepository.GetRequiredActionGroupByActionIdAsync(action.AgvActionCorrelationId);
                if (actionGroup.Actions.Single(a => a.CorrelationId == action.AgvActionCorrelationId).CompletedDate != null)
                {
                    _lastEvaluatedActions.Add(action.AgvActionCorrelationId);
                }
            }
        }
    }

    private async Task<ICollection<Station>> GetAvailableStationsForCoordination()
    {
        ICollection<Station> stations = await stationRepository.GetStationsById(_consideredStations.ToList());
        return GetAvailableStations(stations);
    }

    /// <summary>
    /// True, if the target selector would select this station.
    /// </summary>
    public async Task<bool> WouldSelectStation(Guid stationId)
    {
        ICollection<Station> availableStations = await GetAvailableStationsForCoordination();

        foreach (var station in availableStations)
        {
            if (!station.IsEmpty)
            {
                continue;
            }
            if (_useJamState)
            {
                if (station.HasPalletJam)
                {
                    continue;
                }
            }
            return station.Id == stationId;
        }

        return false;
    }
}