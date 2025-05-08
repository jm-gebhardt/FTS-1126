using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Domain.Services;

/// <inheritdoc cref="TargetSelector"/>>
[UsedImplicitly]
public class TargetSelector(
    ILogger<TargetSelector> logger,
    IStationRepository stationRepository,
    KaeserGraphConfiguration configuration,
    ITransportOrderDb transportOrderDb,
    IDecisionPointActionRepository decisionPointActionRepository,
    ISemaphoreProvider<ITargetSelectionSemaphore> semaphoreProvider,
    IBus bus) : BackgroundService, ITargetSelector
{
    private readonly List<Guid> _lastEvaluatedActions = [];
    private readonly ConcurrentDictionary<Guid, DateTime> _actionsToSelectTarget = new();
    private int _consideredStations = 3;
    private readonly TimeSpan _waitingTime = TimeSpan.FromMilliseconds(500);

    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Initialize();
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_actionsToSelectTarget.IsEmpty)
            {
                logger.LogDebug("No action listed for target selection ");
                await Task.Delay(_waitingTime, stoppingToken);
                continue;
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

                    string? target = await SelectTarget(_consideredStations);
                    if (target != null)
                    {
                        logger.LogInformation("Selecting target {Target} for agv {Agv} running action group {ActionGroup}", target, agvCorrelationId, actionGroup.CorrelationId);
                        
                        // occupy station when target is selected already, so that we can use the occupancy status to select future targets
                        await stationRepository.OccupyStation(target, agvCorrelationId);
                        _lastEvaluatedActions.Add(action.Value);
                        await bus.Publish(new SetTargetForAction(action.Value, actionGroup.CorrelationId, agvCorrelationId, target), stoppingToken);
                        var decisionPointAction = await decisionPointActionRepository.GetByIdAsync(action.Value);
                        decisionPointAction.SetTarget(target);
                        await decisionPointActionRepository.UpdateAsync(decisionPointAction);
                    }
                }
            }, stoppingToken);
            await Task.Delay(_waitingTime, stoppingToken);
        }
    }

    /// <inheritdoc cref="TargetSelector"/>>
    public void SetStationCount(int stationCount)
    {
        _consideredStations = stationCount;
    }

    private async Task<string?> SelectTarget(int consideredStations)
    {
        string? selectedTarget = null;
        ICollection<StationOccupancy> occupancies = await stationRepository.GetStationOccupancy();
        ICollection<TransportOrder.Domain.Models.Station.Station> stations = await transportOrderDb.Stations.GetStationsByNames(configuration.ConveyorStationMapping.Keys.ToArray());

        //Try to take the station that is not occupied, is not locked, and was not chosen last (to compensate for the delay between setting target and starting action)
        ICollection<StationOccupancy> availableStations = GetAvailableStations(consideredStations, occupancies, stations);
        foreach (var occupancy in availableStations)
        {
            if (!occupancy.IsEmpty)
            {
                logger.LogInformation("Station: {Station} is not free, as it's occupied by {occupancy.Count}, StationCount set to {StationCount}", occupancy.Station, occupancy.Count, consideredStations);
                continue;
            }
            selectedTarget = occupancy.Station;
            break;
        }
        return selectedTarget;
    }

    private ICollection<StationOccupancy> GetAvailableStations(int consideredStations, ICollection<StationOccupancy> unorderedOccupancies, ICollection<TransportOrder.Domain.Models.Station.Station> stations)
    {
        // By agreement conveyor stations are named "PS01", "PS02", "PS03", thus ordering by conveyor station
        // name ensures priority of PS01 over PS02 and PS03
        IOrderedEnumerable<KeyValuePair<string, string>> orderedConveyorStations = configuration.ConveyorStationMapping.OrderBy(m => m.Value);
        ICollection<StationOccupancy> orderedOccupancies = [];
        foreach (KeyValuePair<string, string> conveyorStationMapping in orderedConveyorStations.Take(consideredStations))
        {
            var occupancy = unorderedOccupancies.SingleOrDefault(o => o.Station == conveyorStationMapping.Key) ??
                            new StationOccupancy(conveyorStationMapping.Key, 0, null);

            if (!stations.First(s => s.Name == occupancy.Station).IsLocked)
            {
                orderedOccupancies.Add(occupancy);
            }
        }
        return orderedOccupancies;
    }

    /// <inheritdoc cref="ITargetSelector"/>>
    public async Task Enqueue(Guid actionCorrelationId, Guid agvCorrelationId)
    {
        var now = DateTime.UtcNow;
        logger.LogInformation("Listing action {ActionCorrelationId} for target selection", actionCorrelationId);
        _actionsToSelectTarget.TryAdd(actionCorrelationId, now);
        var decisionPointAction = new DecisionPointAction(actionCorrelationId, agvCorrelationId, now);
        await decisionPointActionRepository.AddAsync(decisionPointAction);
        await decisionPointActionRepository.SaveAsync();
    }

    /// <inheritdoc cref="ITargetSelector"/>>
    public async Task Dequeue(Guid actionCorrelationId)
    {
        // Try to dequeue directly this action first. This happens if it was a directly created action that did not have its type changed on the way (e.g. move to pick)
        if (_actionsToSelectTarget.ContainsKey(actionCorrelationId))
        {
            await RemoveAction(actionCorrelationId);
            return;
        }

        // If not found, search in action groups for the original (skipped) action
        logger.LogInformation("Trying to dequeue original action {ActionCorrelationId}", actionCorrelationId);
        var actionGroup = await transportOrderDb.ActionGroups.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);
        var skippedAction = actionGroup.Actions.SingleOrDefault(a => a.ShouldSkip);

        if (skippedAction == null || !_actionsToSelectTarget.ContainsKey(skippedAction.CorrelationId))
        {
            logger.LogWarning("Could not find action {ActionCorrelationId} or original to dequeue", actionCorrelationId);
            return;
        }

        await RemoveAction(skippedAction.CorrelationId);
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
        ICollection<DecisionPointAction> actions = await decisionPointActionRepository.GetAll();

        foreach (var action in actions.Where(a => a.Target == null))
        {
            _actionsToSelectTarget.TryAdd(action.CorrelationId, action.EntryTime);
        }

        foreach (var action in actions.Where(a => a.Target != null))
        {
            var actionGroup = await transportOrderDb.ActionGroups.GetRequiredActionGroupByActionIdAsync(action.CorrelationId);
            if (actionGroup.Actions.Single(a => a.CorrelationId == action.CorrelationId).CompletedDate != null)
            {
                _lastEvaluatedActions.Add(action.CorrelationId);
            }
        }
    }
}