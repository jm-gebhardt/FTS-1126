using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;

/// <summary>
/// Handles Short Loop Waiting (SLW) action with consideration of long loop order priority.
/// </summary>
public class PriorityAwareWaitingHandler(
    ILogger<PriorityAwareWaitingHandler> logger,
    IStationRepository stationRepository,
    IActionGroupRepository actionGroupRepository,
    IWaitingPointActionRepository waitingPointActionRepository,
    ISemaphoreProvider<IWaitingHandlerSemaphore> semaphoreProvider,
    INodeRepository nodeRepository,
    IBus bus) : IWaitingHandler
{
    private readonly ConcurrentDictionary<Guid, DateTime> _actionsToSelectTarget = new();
    private readonly HashSet<Guid> _lastEvaluatedActions = new();
    private Guid _primaryStation = "PS03".ToGuid();
    private ICollection<Guid> _secondaryStations = ["PS02".ToGuid(), "PS01".ToGuid()];

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
            var action = _actionsToSelectTarget
                .Where(kvp => !_lastEvaluatedActions.Contains(kvp.Key))
                .OrderBy(kvp => kvp.Value)
                .Select(kvp => (Guid?) kvp.Key)
                .FirstOrDefault();

            if (action == null)
            {
                logger.LogDebug("No short loop waiting action found");
                return;
            }

            var actionGroup = await actionGroupRepository.GetRequiredActionGroupByActionIdAsync(action.Value);
            var agvCorrelationId = actionGroup.AssignedAgv!.Value;
            logger.LogInformation("Handle waiting for agv {Agv} running action group {ActionGroup}", agvCorrelationId, actionGroup.CorrelationId);

            var target = await GetCurrentTargetStation();
            if (target == null)
            {
                logger.LogWarning("No target for waiting found");
                return;
            }

            if (!target.IsEmpty)
            {
                logger.LogWarning("Target for waiting is not empty {Target}", target.Name);
                return;
            }

            if (target.HasPalletJam)
            {
                logger.LogInformation("Station {Target} not available due to pallet jam", target.Name);
                return;
            }

            if (await skipForLongLoopOrder(action.Value))
            {
                logger.LogInformation("Skipping SLW action {Action} as there is a long loop order that has priority", action.Value);
                return;
            }

            await AssignTarget(action.Value, actionGroup, agvCorrelationId, target.Id, stoppingToken);
        }, stoppingToken);
    }

    private async Task<Station?> GetCurrentTargetStation()
    {
        var target = await stationRepository.GetStation(_primaryStation);
        if (target.IsLocked)
        {
            logger.LogInformation("Station {Target} is locked, trying to use alternative target", target.Name);
            var targets = await stationRepository.GetStationsById(_secondaryStations);
            var alternativeTarget = targets.FirstOrDefault(t => !t.IsLocked);
            if (alternativeTarget == null)
            {
                logger.LogWarning("All target stations for waiting handling locked");
                return null;
            }
            target = alternativeTarget;
        }
        return target;
    }

    private async Task AssignTarget(Guid actionId, ActionGroup actionGroup, Guid agvCorrelationId, Guid targetNodeId, CancellationToken stoppingToken)
    {
        var node = await nodeRepository.GetNodeAsync(targetNodeId);
        node.Reserve(agvCorrelationId);
        if (await nodeRepository.UpdateLock(node))
        {
            _lastEvaluatedActions.Add(actionId);
            logger.LogInformation("Adding agv {Agv} to station {Station} for short loop waiting", agvCorrelationId, targetNodeId);
            if (node.Lock != null)
            {
                await bus.Publish(new NodeStateChanged(node.Lock!, node.Name, targetNodeId, null, null, null), stoppingToken);
            }
            await bus.Publish(new SetTargetForAction(actionId, actionGroup.CorrelationId, agvCorrelationId, targetNodeId), stoppingToken);

            var waitingPointAction = await waitingPointActionRepository.GetByIdAsync(actionId);
            waitingPointAction.SetTarget(targetNodeId);
            await waitingPointActionRepository.UpdateAsync(waitingPointAction);
        }
    }

    /// <summary>
    /// Get the short loop target station.
    /// </summary>
    /// <returns>The node id of the target.</returns>
    public async Task<Guid> GetTargetStation()
    {
        return (await GetCurrentTargetStation())?.Id ?? Guid.Empty;
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

    /// <inheritdoc cref="IWaitingHandler"/>
    public async Task StopHandling(Guid actionCorrelationId)
    {
        if (_actionsToSelectTarget.ContainsKey(actionCorrelationId))
        {
            await RemoveAction(actionCorrelationId);
            return;
        }

        logger.LogInformation("Trying to stop handling SLW action {ActionCorrelationId}", actionCorrelationId);
        var actionGroup = await actionGroupRepository.GetRequiredActionGroupByActionIdAsync(actionCorrelationId);
        var skippedAction = actionGroup.Actions.SingleOrDefault(a => a.ShouldSkip);

        if (skippedAction == null || !_actionsToSelectTarget.ContainsKey(skippedAction.CorrelationId))
        {
            logger.LogWarning("Could not find SLW action {ActionCorrelationId} or skipped action on action group {ActionGroupCorrelationId}",
                actionCorrelationId, actionGroup.CorrelationId);
            return;
        }

        await RemoveAction(skippedAction.CorrelationId);
    }

    /// <inheritdoc cref="IWaitingHandler"/>
    public Task<ICollection<Guid>> GetConfiguredTargets()
    {
        ICollection<Guid> targets = [_primaryStation, .._secondaryStations];
        return Task.FromResult(targets);
    }

    /// <inheritdoc cref="IWaitingHandler"/>
    public Task ConfigureTargets(Guid primaryTarget, ICollection<Guid> secondaryTargets)
    {
        _primaryStation = primaryTarget;
        _secondaryStations = secondaryTargets;
        return Task.CompletedTask;
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
        var waitingPointActions = await waitingPointActionRepository.GetAll();

        foreach (var action in waitingPointActions)
        {
            if (action.TargetNodeId == null)
            {
                _actionsToSelectTarget.TryAdd(action.AgvActionCorrelationId, action.CreatedTime);
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
}