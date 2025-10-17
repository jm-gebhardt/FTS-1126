using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.KaeserSidecar.Domain;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Gebhardt.Acc.KaeserSidecar.Services.BackgroundServices;

/// <summary>
/// Handles Short Loop Waiting (SLW) action with consideration of long loop order priority.
/// </summary>
public class PriorityAwareWaitingHandler(
    ILogger<PriorityAwareWaitingHandler> logger,
    IStationRepository stationRepository,
    ITransportOrderDb transportOrderDb,
    IWaitingPointActionRepository waitingPointActionRepository,
    ISemaphoreProvider<IWaitingHandlerSemaphore> semaphoreProvider,
    IPrismatCommunicationShortLoopWeTargetStationRepository shortLoopTargetStationRepository,
    IJamStateRepository jamStateRepository,
    IBus bus) : IWaitingHandler
{
    private readonly ConcurrentDictionary<Guid, DateTime> _actionsToSelectTarget = new();
    private readonly HashSet<Guid> _lastEvaluatedActions = new();

    /// <summary>
    /// Pending actions.
    /// </summary>
    public IReadOnlyDictionary<Guid, DateTime> PendingActions => _actionsToSelectTarget;

    /// <summary>
    /// Actions that were already evaluated.
    /// </summary>
    public IReadOnlyCollection<Guid> EvaluatedActions => _lastEvaluatedActions;

    /// <summary>
    /// True, if there are no waiting actions.
    /// </summary>
    public bool IsEmpty => _actionsToSelectTarget.IsEmpty;

    /// <summary>
    /// Process pending actions.
    /// </summary>
    /// <param name="skipForLongLoopOrder">Callback for checking long loop order priority.</param>
    /// <param name="stoppingToken">The cancellation token.</param>
    public async Task ProcessActions(Func<Guid, Task<bool>> skipForLongLoopOrder, CancellationToken stoppingToken)
    {
        if (_actionsToSelectTarget.IsEmpty)
        {
            logger.LogDebug("No short loop waiting action listed");
            return;
        }
        await semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            Guid? action = _actionsToSelectTarget
            .Where(kvp => !_lastEvaluatedActions.Contains(kvp.Key))
            .OrderBy(kvp => kvp.Value)
            .Select(kvp => (Guid?)kvp.Key)
            .FirstOrDefault();

        if (action == null)
        {
            logger.LogDebug("No short loop waiting action found");
            return;
        }

        var actionGroup = await transportOrderDb.ActionGroups.GetRequiredActionGroupByActionIdAsync(action.Value);
        var agvCorrelationId = actionGroup.AssignedAgv!.Value;
        logger.LogInformation("Handle waiting for agv {Agv} running action group {ActionGroup}", agvCorrelationId, actionGroup.CorrelationId);

        var target = await shortLoopTargetStationRepository.GetTargetStation();
        var targetStation = target.Current.ToGuid();
        if (target.IsLocked)
        {
            logger.LogInformation("Station {Target} is locked", targetStation);
                return;
        }
        
        var occupancy = await stationRepository.GetOccupancyOfStation(targetStation);

        if (!occupancy.IsEmpty)
        {
            return;
        }

        bool hasPalletJam = await GetJamState(targetStation);
        if (hasPalletJam)
        {
            logger.LogInformation("Station {Target} not available due to pallet jam", occupancy.StationId);
            return;
        }

        if (await skipForLongLoopOrder(action.Value))
        {
            logger.LogInformation("Skipping SLW action {Action} as there is a long loop order that has priority", action.Value);
            return;
        }

        await AssignTarget(action.Value, actionGroup, agvCorrelationId, targetStation, stoppingToken);
        }, stoppingToken);
    }

    private async Task AssignTarget(Guid actionId, ActionGroup actionGroup, Guid agvCorrelationId, Guid target, CancellationToken stoppingToken)
    {
        _lastEvaluatedActions.Add(actionId);
        logger.LogInformation("Adding agv {Agv} to station {Station} for short loop waiting", agvCorrelationId, target);
        await stationRepository.OccupyStation(target, agvCorrelationId);
        await bus.Publish(new SetTargetForAction(actionId, actionGroup.CorrelationId, agvCorrelationId, target), stoppingToken);
        await bus.Publish(new StationReserved(target, agvCorrelationId), stoppingToken);

        var waitingPointAction = await waitingPointActionRepository.GetByIdAsync(actionId);
        waitingPointAction.SetTarget(target);
        await waitingPointActionRepository.UpdateAsync(waitingPointAction);
    }

    private async Task<bool> GetJamState(Guid stationId)
    {
        var jamState = await jamStateRepository.GetByStationAsync(stationId) ?? new JamState(stationId, 0, DateTime.UtcNow);
        return jamState.HasPalletJam;
    }

    /// <summary>
    /// Get the short loop target station.
    /// </summary>
    /// <returns>The id of the target.</returns>
    public async Task<Guid> GetTargetStation()
    {
        return (await shortLoopTargetStationRepository.GetTargetStation()).Current.ToGuid();
    }

    /// <inheritdoc cref="IWaitingHandler"/>>
    public async Task StartHandling(Guid actionCorrelationId, Guid agvCorrelationId, DateTime orderCreationTime)
    {
        if (!_actionsToSelectTarget.ContainsKey(actionCorrelationId))
        {
            logger.LogInformation("Listing SLW waiting action {ActionCorrelationId}", actionCorrelationId);
            _actionsToSelectTarget.TryAdd(actionCorrelationId, orderCreationTime);
            var waitingPointAction = new WaitingPointAction(actionCorrelationId, agvCorrelationId, orderCreationTime);
            await waitingPointActionRepository.AddAsync(waitingPointAction);
            return;
        }
        logger.LogInformation("SLW action {ActionCorrelationId} already listed for waiting, not listing again", actionCorrelationId);
    }

    /// <inheritdoc cref="IWaitingHandler"/>>
    public async Task StopHandling(Guid actionCorrelationId)
    {
        if (_actionsToSelectTarget.ContainsKey(actionCorrelationId))
        {
            await RemoveAction(actionCorrelationId);
            return;
        }

        logger.LogInformation("Trying to stop handling SLW action {ActionCorrelationId}", actionCorrelationId);
        var actionGroup = await transportOrderDb.ActionGroups.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);
        var skippedAction = actionGroup.Actions.SingleOrDefault(a => a.ShouldSkip);

        if (skippedAction == null || !_actionsToSelectTarget.ContainsKey(skippedAction.CorrelationId))
        {
            logger.LogWarning("Could not find SLW action {ActionCorrelationId} or skipped action on action group {ActionGroupCorrelationId}",
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
            logger.LogInformation("Removed SLW action {ActionCorrelationId} from waiting handling", correlationId);
            await waitingPointActionRepository.RemoveById(correlationId);
        }
    }

    /// <inheritdoc cref="IWaitingHandler"/>>
    public async Task Initialize()
    {
        ICollection<WaitingPointAction> waitingPointActions = await waitingPointActionRepository.GetAll();

        foreach (var action in waitingPointActions)
        {
            if (action.Target == null)
            {
                _actionsToSelectTarget.TryAdd(action.CorrelationId, action.CreatedTime);
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