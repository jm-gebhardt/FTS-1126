using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.KaeserSidecar.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

namespace Gebhardt.Acc.KaeserSidecar.Services.BackgroundServices;

/// <summary>
/// Handles Decision Point 1 (DP1) target selection logic with consideration of short loop order priority.
/// </summary>
public class PriorityAwareTargetSelector(
    ILogger<PriorityAwareTargetSelector> logger,
    IStationRepository stationRepository,
    KaeserGraphConfiguration configuration,
    ITransportOrderDb transportOrderDb,
    ISemaphoreProvider<ITargetSelectionSemaphore> semaphoreProvider,
    IDecisionPointActionRepository decisionPointActionRepository,
    IJamStateRepository jamStateRepository,
    IBus bus)
    : ITargetSelector
{
    private readonly ConcurrentDictionary<Guid, DateTime> _actionsToSelectTarget = new();
    private readonly List<Guid> _lastEvaluatedActions = [];
    private ICollection<Guid> _consideredStations = configuration.ConveyorStationMapping.Keys.Order().Take(2).Select(s => s.ToGuid()).ToList();
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

            var actionGroup = await transportOrderDb.ActionGroups.GetRequiredActionGroupByActionIdAsync(action.Value);
            if (_lastEvaluatedActions.Contains(action.Value))
            {
                return;
            }

            var agvCorrelationId = actionGroup.AssignedAgv!.Value;
            logger.LogInformation("Trigger target selection for agv {Agv} running action group {ActionGroup}", agvCorrelationId, actionGroup.CorrelationId);

            Guid? stationId = await SelectTarget(_consideredStations, action.Value, skipForShortLoopOrder);
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

    private async Task<Guid?> SelectTarget(ICollection<Guid> consideredStations, Guid actionId, Func<Guid, bool> shouldSkipStation)
    {
        StationOccupancy? selectedTarget = null;
        ICollection<StationOccupancy> occupancies = await stationRepository.GetStationOccupancy();
        ICollection<TransportOrder.Domain.Models.Station.Station> stations = await transportOrderDb.Stations.GetStationsByIdsAsync(consideredStations.ToList());

        ICollection<StationOccupancy> availableStations = GetAvailableStations(occupancies, stations);

        foreach (var occupancy in availableStations)
        {
            if (!occupancy.IsEmpty)
            {
                logger.LogInformation("Station: {Station} is not free, as it's occupied by {OccupancyCount} agv, StationCount set to {StationCount}",
                    occupancy.StationId, occupancy.Count, consideredStations);
                continue;
            }

            if (_useJamState)
            {
                string jamStateStation = configuration.ConveyorStationMapping.Single(m => m.Key.ToGuid() == occupancy.StationId).Key;
                logger.LogInformation("Considering jam state for station {Target}", occupancy.StationId);
                bool hasPalletJam = await GetJamState(jamStateStation.ToGuid());
                if (hasPalletJam)
                {
                    logger.LogInformation("Station {Target} not available due to pallet jam", occupancy.StationId);
                    continue;
                }
            }

            if (shouldSkipStation(occupancy.StationId))
            {
                logger.LogInformation("Skipping station {Station} for DP1 action {Action} as there is a short loop order that has priority", occupancy.StationId, actionId);
                continue;
            }

            selectedTarget = occupancy;
            break;
        }

        return selectedTarget?.StationId;
    }

    private async Task AssignTarget(Guid actionId, ActionGroup actionGroup, Guid agvCorrelationId, Guid stationId, CancellationToken stoppingToken)
    {
        logger.LogInformation("Selected target {Target} for agv {Agv} running action group {ActionGroup} and adding to station",
            stationId, agvCorrelationId, actionGroup.CorrelationId);

        await stationRepository.OccupyStation(stationId, agvCorrelationId);
        _lastEvaluatedActions.Add(actionId);
        await bus.Publish(new SetTargetForAction(actionId, actionGroup.CorrelationId, agvCorrelationId, stationId), stoppingToken);
        await bus.Publish(new StationReserved(stationId, agvCorrelationId), stoppingToken);

        var decisionPointAction = await decisionPointActionRepository.GetByIdAsync(actionId);
        decisionPointAction.SetTarget(stationId);
        await decisionPointActionRepository.UpdateAsync(decisionPointAction);
    }

    private async Task<bool> GetJamState(Guid stationId)
    {
        var jamState = await jamStateRepository.GetByStationAsync(stationId) ?? new JamState(stationId, 0, DateTime.UtcNow);
        return jamState.HasPalletJam;
    }

    private ICollection<StationOccupancy> GetAvailableStations(ICollection<StationOccupancy> unorderedOccupancies,
        ICollection<TransportOrder.Domain.Models.Station.Station> stations)
    {
        IOrderedEnumerable<KeyValuePair<string, string>> orderedConveyorStations = configuration.ConveyorStationMapping.OrderBy(m => m.Value);
        ICollection<StationOccupancy> orderedOccupancies = [];
        foreach (KeyValuePair<string, string> conveyorStationMapping in orderedConveyorStations.Where(o => stations.Select(s => s.Name).ToList().Contains(o.Key)))
        {
            var occupancy = unorderedOccupancies.SingleOrDefault(o => o.StationId == conveyorStationMapping.Key.ToGuid()) ??
                            new StationOccupancy(conveyorStationMapping.Key.ToGuid(), 0, null);

            if (!stations.First(s => s.Id == occupancy.StationId).IsLocked)
            {
                orderedOccupancies.Add(occupancy);
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
        var actionGroup = await transportOrderDb.ActionGroups.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);
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
            _consideredStations = decisionPointActions.MaxBy(a => a.EntryTime)?.ConsideredStations ?? configuration.ConveyorStationMapping.Keys.Order().Take(2).Select(s => s.ToGuid()).ToList();
        }

        foreach (var action in decisionPointActions)
        {
            if (action.Target == null)
            {
                _actionsToSelectTarget.TryAdd(action.CorrelationId, action.EntryTime);
            }
            else
            {
                var actionGroup = await transportOrderDb.ActionGroups.GetRequiredActionGroupByActionIdAsync(action.CorrelationId);
                if (actionGroup.Actions.Single(a => a.CorrelationId == action.CorrelationId).CompletedDate != null)
                {
                    _lastEvaluatedActions.Add(action.CorrelationId);
                }
            }
        }
    }

    private async Task<ICollection<StationOccupancy>> GetAvailableStationsForCoordination()
    {
        ICollection<StationOccupancy> occupancies = await stationRepository.GetStationOccupancy();
        ICollection<TransportOrder.Domain.Models.Station.Station> stations = await transportOrderDb.Stations.GetStationsByIdsAsync(_consideredStations.ToList());
        return GetAvailableStations(occupancies, stations);
    }

    /// <summary>
    /// True, if the target selector would select this station.
    /// </summary>
    public async Task<bool> WouldSelectStation(Guid stationId)
    {
        ICollection<StationOccupancy> availableStations = await GetAvailableStationsForCoordination();

        foreach (var occupancy in availableStations)
        {
            if (!occupancy.IsEmpty)
            {
                continue;
            }
            if (_useJamState)
            {
                string jamStateStation = configuration.ConveyorStationMapping.Single(m => m.Key.ToGuid() == occupancy.StationId).Key;
                bool hasPalletJam = await GetJamState(jamStateStation.ToGuid());
                if (hasPalletJam)
                {
                    continue;
                }
            }
            return occupancy.StationId == stationId;
        }

        return false;
    }
}