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
    private string _lastTargetStation = string.Empty;
    private readonly List<Guid> _lastEvaluatedActions = [];
    private readonly ConcurrentDictionary<Guid, DateTime> _actionsToSelectTarget = new();
    private int _consideredStations = 3;
    private readonly TimeSpan _waitingTime = TimeSpan.FromMilliseconds(100);

    /// <inheritdoc cref="BackgroundService"/>>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Initialize();
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_actionsToSelectTarget.IsEmpty)
            {
                logger.LogInformation("No action listed for target selection ");
                await Task.Delay(_waitingTime, stoppingToken);
                continue;
            }
            await semaphoreProvider.ExecuteLockedAsync(async () =>
            {
                Guid? action = null;
                if (_consideredStations == 1)
                {
                    action = _actionsToSelectTarget.Count == 0 ?
                        null :
                        _actionsToSelectTarget.Aggregate((l, r) => l.Value < r.Value ? l : r).Key; // take the one that entered first
                }
                if (_consideredStations > 1)
                {
                    var lastEvaluatedSet = new HashSet<Guid>(_lastEvaluatedActions);
                    action = _actionsToSelectTarget
                        .Where(kvp => !lastEvaluatedSet.Contains(kvp.Key))
                        .OrderBy(kvp => kvp.Value) // order by entry time 
                        .Select(kvp => (Guid?)kvp.Key) // use the action correlation id
                        .FirstOrDefault();
                }
                if (action == null)
                {
                    logger.LogInformation("No action found for target selection ");
                    return;
                }

                var actionGroup = await transportOrderDb.ActionGroups.GetRequiredActionGroupByActionIdAsync(action.Value);
                if (!_lastEvaluatedActions.Contains(action.Value))
                {
                    logger.LogInformation("Trigger target selection for agv {Agv} running action group {ActionGroup}", actionGroup.AssignedAgv!.Value, actionGroup.CorrelationId);

                    string? target = await Select(_consideredStations);
                    if (target != null)
                    {
                        _lastEvaluatedActions.Add(action.Value);
                        logger.LogInformation("Selecting target {Target} for agv {Agv} running action group {ActionGroup}", target, actionGroup.AssignedAgv!.Value, actionGroup.CorrelationId);
                        await bus.Publish(new SetTargetForAction(action.Value, actionGroup.CorrelationId, actionGroup.AssignedAgv!.Value, target), stoppingToken);
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

    /// <inheritdoc cref="TargetSelector"/>>
    private async Task<string?> Select(int consideredStations)
    {
        string? selectedTarget = null;
        ICollection<StationOccupancy> occupancies = await stationRepository.GetStationOccupancy();
        ICollection<TransportOrder.Domain.Models.Station.Station> stations = await transportOrderDb.Stations.GetStationsByNames(configuration.ConveyorStationMapping.Keys.ToArray());

        //Try to take the station that is not occupied, is not locked,  and was not chosen last (to compensate for the delay between setting target and starting action)
        ICollection<StationOccupancy> availableStations = GetAvailableStations(consideredStations, occupancies, stations);
        foreach (var occupancy in availableStations)
        {
            if (occupancy.Count != 0 || (_lastTargetStation == occupancy.Station && availableStations.Count > 1))
            {
                logger.LogInformation("No free station found: {Station} is occupied by {occupancy.Count}, last chosen station {LastTarget}", occupancy.Station, occupancy.Count, _lastTargetStation);
                continue;
            }
            selectedTarget = occupancy.Station;
            break;
        }

        if (selectedTarget != null)
        {
            _lastTargetStation = selectedTarget;
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
        decisionPointActionRepository.Add(decisionPointAction);
        await decisionPointActionRepository.SaveAsync();
    }

    /// <inheritdoc cref="ITargetSelector"/>>
    public async Task Dequeue(Guid actionCorrelationId)
    {
        if (!_actionsToSelectTarget.TryRemove(actionCorrelationId, out var dequeuedAction))
        {
            logger.LogWarning("Dequeued {Dequeued}, expected {Expected}", dequeuedAction, actionCorrelationId);
            var actionGroup = await transportOrderDb.ActionGroups.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);
            var action = actionGroup.Actions.SingleOrDefault(a => a.ShouldSkip);
            if (action == null)
            {
                return;
            }
            logger.LogInformation("Dequeued {Dequeued}", action.CorrelationId);
            _lastEvaluatedActions.Remove(action.CorrelationId);
            _actionsToSelectTarget.TryRemove(action.CorrelationId, out var _);
            await decisionPointActionRepository.RemoveById(action.CorrelationId);
            return;
        }
        logger.LogInformation("Dequeued {Dequeued}", actionCorrelationId);
        _lastEvaluatedActions.Remove(actionCorrelationId);
        await decisionPointActionRepository.RemoveById(actionCorrelationId);
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

        _lastTargetStation = actions.Where(a => a.Target != null).OrderBy(a => a.EntryTime).LastOrDefault()?.Target ?? string.Empty;
    }
}