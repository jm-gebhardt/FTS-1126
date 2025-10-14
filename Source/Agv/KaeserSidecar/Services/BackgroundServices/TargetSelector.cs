using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.KaeserSidecar.Domain;
using Gebhardt.Acc.KaeserSidecar.Domain.Constants;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Services.BackgroundServices;

/// <inheritdoc cref="TargetSelector"/>>
[UsedImplicitly]
public class TargetSelector(
    ILogger<TargetSelector> logger,
    IStationRepository stationRepository,
    KaeserGraphConfiguration configuration,
    ITransportOrderDb transportOrderDb,
    IDecisionPointActionRepository decisionPointActionRepository,
    ISemaphoreProvider<ITargetSelectionSemaphore> semaphoreProvider,
    IJamStateRepository jamStateRepository,
    IBus bus) : BackgroundService, ITargetSelector

{
    private readonly List<Guid> _lastEvaluatedActions = [];
    private readonly ConcurrentDictionary<Guid, DateTime> _actionsToSelectTarget = new();
    private int _consideredStations = TargetSelection.DefaultStationCount;
    private bool _useJamState;

    /// <summary>
    /// The time to wait between two target selection attempts.
    /// </summary>
    public readonly TimeSpan WaitingTime = TimeSpan.FromMilliseconds(500);

    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Initialize();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error during target selection");
            }
            finally
            {
                await Task.Delay(WaitingTime, stoppingToken);
            }
        }
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        if (_actionsToSelectTarget.IsEmpty)
        {
            logger.LogDebug("No action listed for target selection ");
            return;
        }

        await semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            Guid? action = null;
            if (_consideredStations == 1 && !_actionsToSelectTarget.IsEmpty)
            {
                action = _actionsToSelectTarget.MinBy(a => a.Value).Key; // take the one that entered first
            }
            if (_consideredStations > 1)
            {
                var lastEvaluatedSet = new HashSet<Guid>(_lastEvaluatedActions);
                action = _actionsToSelectTarget
                    .Where(kvp => !lastEvaluatedSet.Contains(kvp.Key))
                    .OrderBy(kvp => kvp.Value) // order by entry time 
                    .Select(kvp => (Guid?) kvp.Key) // use the action correlation id
                    .FirstOrDefault();
            }
            if (action == null)
            {
                logger.LogDebug("No action found for target selection ");
                return;
            }

            var actionGroup = await transportOrderDb.ActionGroups.GetRequiredActionGroupByActionIdAsync(action.Value);
            if (!_lastEvaluatedActions.Contains(action.Value))
            {
                var agvCorrelationId = actionGroup.AssignedAgv!.Value;
                logger.LogInformation("Trigger target selection for agv {Agv} running action group {ActionGroup}", agvCorrelationId, actionGroup.CorrelationId);

                Guid? stationId = await SelectTarget(_consideredStations);
                if (stationId != null)
                {
                    logger.LogInformation("Selected target {Target} for agv {Agv} running action group {ActionGroup} and adding to station", stationId, agvCorrelationId, actionGroup.CorrelationId);
                    // occupy station when target is selected already, so that we can use the occupancy status to select future targets
                    await stationRepository.OccupyStation(stationId.Value, agvCorrelationId);
                    _lastEvaluatedActions.Add(action.Value);
                    await bus.Publish(new SetTargetForAction(action.Value, actionGroup.CorrelationId, agvCorrelationId, stationId.Value), stoppingToken);
                    await bus.Publish(new StationReserved(stationId.Value, agvCorrelationId), stoppingToken);
                    var decisionPointAction = await decisionPointActionRepository.GetByIdAsync(action.Value);
                    decisionPointAction.SetTarget(stationId.Value);
                    await decisionPointActionRepository.UpdateAsync(decisionPointAction);
                }
            }
        }, stoppingToken);
    }

    /// <inheritdoc cref="TargetSelector"/>>
    public void SetStationCount(int stationCount)
    {
        _consideredStations = stationCount;
    }

    private async Task<Guid?> SelectTarget(int consideredStations)
    {
        StationOccupancy? selectedTarget = null;
        ICollection<StationOccupancy> occupancies = await stationRepository.GetStationOccupancy();
        ICollection<TransportOrder.Domain.Models.Station.Station> stations = await transportOrderDb.Stations.GetStationsByIdsAsync(configuration.ConveyorStationMapping.Keys.Select(k => k.ToGuid()).ToList());

        //Try to take the station that is not occupied, is not locked, and was not chosen last (to compensate for the delay between setting target and starting action)
        // If jam state is used, the station is only considered available without a jam
        ICollection<StationOccupancy> availableStations = GetAvailableStations(consideredStations, occupancies, stations);
        foreach (var occupancy in availableStations)
        {
            if (!occupancy.IsEmpty)
            {
                logger.LogInformation("Station: {StationId} is not free, as it's occupied by {OccupancyCount} agv, StationCount set to {StationCount}", occupancy.StationId, occupancy.Count, consideredStations);
                continue;
            }
            if (_useJamState)
            {
                string jamStateStation = configuration.ConveyorStationMapping.Single(m => m.Key.ToGuid() == occupancy.StationId).Value;
                logger.LogInformation("Considering jam state for station {Target}", occupancy.StationId);
                bool hasPalletJam = await GetJamState(jamStateStation.ToGuid());
                if (hasPalletJam)
                {
                    logger.LogInformation("Station {Target} not available due to pallet jam ", occupancy.StationId);
                    continue;
                }
            }
            selectedTarget = occupancy;
            break;
        }
        return selectedTarget?.StationId;
    }

    private async Task<bool> GetJamState(Guid stationId)
    {
        var jamState = await jamStateRepository.GetByStationAsync(stationId) ?? new JamState(stationId, 0, DateTime.UtcNow);
        return jamState.HasPalletJam;
    }

    private ICollection<StationOccupancy> GetAvailableStations(int consideredStations, ICollection<StationOccupancy> unorderedOccupancies, ICollection<TransportOrder.Domain.Models.Station.Station> stations)
    {
        // By agreement conveyor stations are named "PS01", "PS02", "PS03", thus ordering by conveyor station
        // name ensures priority of PS01 over PS02 and PS03
        IOrderedEnumerable<KeyValuePair<string, string>> orderedConveyorStations = configuration.ConveyorStationMapping.OrderBy(m => m.Value);
        ICollection<StationOccupancy> orderedOccupancies = [];
        foreach (KeyValuePair<string, string> conveyorStationMapping in orderedConveyorStations.Take(consideredStations))
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
    public Task Enqueue(Guid actionCorrelationId, Guid agvCorrelationId)
    {
        return semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var now = DateTime.UtcNow;
            logger.LogInformation("Listing action {ActionCorrelationId} for target selection", actionCorrelationId);
            _actionsToSelectTarget.TryAdd(actionCorrelationId, now);
            var decisionPointAction = new DecisionPointAction(actionCorrelationId, agvCorrelationId, now, _consideredStations);
            await decisionPointActionRepository.AddAsync(decisionPointAction);
        });
    }

    /// <inheritdoc cref="ITargetSelector"/>>
    public Task Dequeue(Guid actionCorrelationId)
    {
        return semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            // Try to dequeue directly this action first. This happens if it was a directly created action that did not have its type changed on the way (e.g. move to pick)
            if (_actionsToSelectTarget.ContainsKey(actionCorrelationId))
            {
                await RemoveAction(actionCorrelationId);
                return;
            }

            // If not found, search in action groups for the original (skipped) action
            logger.LogInformation("Trying to dequeue action {ActionCorrelationId}", actionCorrelationId);
            var actionGroup = await transportOrderDb.ActionGroups.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);
            var skippedAction = actionGroup.Actions.SingleOrDefault(a => a.ShouldSkip);

            if (skippedAction == null || !_actionsToSelectTarget.ContainsKey(skippedAction.CorrelationId))
            {
                logger.LogWarning("Could not find action {ActionCorrelationId} or skipped action on action group {ActionGroupCorrelationId}", actionCorrelationId, actionGroup.CorrelationId);
                return;
            }

            await RemoveAction(skippedAction.CorrelationId);
        });
    }

    /// <inheritdoc cref="ITargetSelector"/>>
    public void SetUseJamState(bool useJamState)
    {
        _useJamState = useJamState;
    }

    private async Task RemoveAction(Guid correlationId)
    {
        if (_actionsToSelectTarget.TryRemove(correlationId, out _))
        {
            _lastEvaluatedActions.Remove(correlationId);
            logger.LogInformation("Dequeued action {ActionCorrelationId}", correlationId);
            await decisionPointActionRepository.RemoveById(correlationId);
        }
    }

    private async Task Initialize()
    {
        ICollection<DecisionPointAction> decisionPointActions = await decisionPointActionRepository.GetAll();

        if (decisionPointActions.Count > 0)
        {
            _consideredStations = decisionPointActions.MaxBy(a => a.EntryTime)?.StationCount ?? TargetSelection.DefaultStationCount;
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
}