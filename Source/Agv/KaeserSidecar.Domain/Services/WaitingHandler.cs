using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Messages.Stations.Events;

namespace Gebhardt.Acc.KaeserSidecar.Domain.Services;

/// <inheritdoc cref="IWaitingHandler"/>>
public class WaitingHandler(
    ILogger<WaitingHandler> logger,
    IStationRepository stationRepository,
    ITransportOrderDb transportOrderDb,
    IWaitingPointActionRepository waitingPointActionRepository,
    IPrismatCommunicationShortLoopWeTargetStationRepository shortLoopTargetStationRepository,
    ISemaphoreProvider<IWaitingHandlerSemaphore> semaphoreProvider,
    IBus bus) : BackgroundService, IWaitingHandler
{
    /// <summary>
    /// The time to wait between two handling attempts.
    /// </summary>
    public readonly TimeSpan WaitingTime = TimeSpan.FromMilliseconds(500);

    private readonly HashSet<Guid> _lastEvaluatedActions = new();
    private readonly ConcurrentDictionary<Guid, DateTime> _actionsToSelectTarget = new();

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
                logger.LogError(e, "Error during handling of short loop waiting");
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
            logger.LogDebug("No waiting action listed");
            return;
        }
        await semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            Guid? action = _actionsToSelectTarget
                .Where(kvp => !_lastEvaluatedActions.Contains(kvp.Key))
                .OrderBy(kvp => kvp.Value) // order by entry time 
                .Select(kvp => (Guid?) kvp.Key) // use the action correlation id
                .FirstOrDefault();
            if (action == null)
            {
                logger.LogDebug("No waiting action found");
                return;
            }
            var actionGroup = await transportOrderDb.ActionGroups.GetRequiredActionGroupByActionIdAsync(action.Value);

            var agvCorrelationId = actionGroup.AssignedAgv!.Value;
            logger.LogInformation("Handle waiting for agv {Agv} running action group {ActionGroup}", agvCorrelationId, actionGroup.CorrelationId);
            string target = (await shortLoopTargetStationRepository.GetTargetStation()).Current;
            var occupancy = await stationRepository.GetOccupancyOfStation(target);
            if (!occupancy.IsEmpty)
            {
                return;
            }
            _lastEvaluatedActions.Add(action.Value);
            // occupy station when target is selected already, so that we can use the occupancy status for future waiting
            logger.LogInformation("Adding agv {Agv} to station {Station} for short loop waiting", agvCorrelationId, target);
            await stationRepository.OccupyStation(target, agvCorrelationId);
            await bus.Publish(new SetTargetForAction(action.Value, actionGroup.CorrelationId, agvCorrelationId, target), stoppingToken);
            await bus.Publish(new StationReserved(target, agvCorrelationId), stoppingToken);
            var waitingPointAction = await waitingPointActionRepository.GetByIdAsync(action.Value);
            waitingPointAction.SetTarget(target);
            await waitingPointActionRepository.UpdateAsync(waitingPointAction);
        }, stoppingToken);
    }

    /// <inheritdoc cref="IWaitingHandler"/>>
    public Task StartHandling(Guid actionCorrelationId, Guid agvCorrelationId, DateTime orderCreationTime)
    {
        return semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            if (!_actionsToSelectTarget.ContainsKey(actionCorrelationId))
            {
                logger.LogInformation("Listing waiting action {ActionCorrelationId}", actionCorrelationId);
                _actionsToSelectTarget.TryAdd(actionCorrelationId, orderCreationTime);
                var waitingPointAction = new WaitingPointAction(actionCorrelationId, agvCorrelationId, orderCreationTime);
                await waitingPointActionRepository.AddAsync(waitingPointAction);
                return;
            }
            logger.LogInformation("Action {ActionCorrelationId} already listed for waiting, not listing again", actionCorrelationId);
        });
    }

    /// <inheritdoc cref="IWaitingHandler"/>>
    public Task StopHandling(Guid actionCorrelationId)
    {
        return semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            // Try to remove this action first. This happens if it was a directly created action that did not have its type changed on the way (e.g. move to pick)
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

    private async Task RemoveAction(Guid correlationId)
    {
        if (_actionsToSelectTarget.TryRemove(correlationId, out _))
        {
            _lastEvaluatedActions.Remove(correlationId);
            logger.LogInformation("Removed action {ActionCorrelationId} from waiting handling", correlationId);
            await waitingPointActionRepository.RemoveById(correlationId);
        }
    }

    private async Task Initialize()
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