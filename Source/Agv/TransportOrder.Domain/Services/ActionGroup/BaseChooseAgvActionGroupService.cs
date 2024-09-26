using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Provides a base class for implementations of <see cref="IChooseAgvActionGroupService"/>
/// </summary>
public abstract class BaseChooseAgvActionGroupService : IChooseAgvActionGroupService
{
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly INodeStateRepository _nodeStateRepository;
    private readonly ILogger<IChooseAgvActionGroupService> _logger;

    /// <inheritdoc cref="IChooseAgvActionGroupService"/>>
    protected BaseChooseAgvActionGroupService(
        IActionGroupRepository actionGroupRepository,
        INodeStateRepository nodeStateRepository,
        ILogger<IChooseAgvActionGroupService> logger
    )
    {
        _logger = logger;
        _actionGroupRepository = actionGroupRepository;
        _nodeStateRepository = nodeStateRepository;
    }

    /// <summary>
    /// Returns the first ActionGroup that satisfies these conditions:
    /// <para>- Is a charging action group (with no locked charging station).</para>
    /// <para>- Is a manual action group</para>
    /// <para>- Is there an action group with a locked action target node then skip this action group.</para>
    /// <para>- Is an action group assigned to the agv selected based on the policy implementing chooser.</para>
    /// <para>- Is an action group selected based on the policy implementing chooser.</para>
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <param name="agvData">The data of the agv.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>ActionGroup</returns>
    /// <exception cref="AgvLocationUnknownException"></exception>
    public async Task<Models.ActionGroup.ActionGroup?> GetNotStartedActionGroupAsync(Agv agv, AgvData? agvData, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string>? lockedNodeIds = await _nodeStateRepository.GetLockedNodes();
        if (lockedNodeIds is { Count: > 0 })
        {
            _logger.LogDebug("Locked nodes: {Ids}", string.Join(",", lockedNodeIds));
        }

        var chargingActionGroup = await _actionGroupRepository.GetCreatedChargingActionGroupForAgvAsync(agv.CorrelationId);
        if (chargingActionGroup != null)
        {
            if (chargingActionGroup.IsManual || lockedNodeIds is { Count: 0 } ||
                (lockedNodeIds is { Count: > 0 } && !chargingActionGroup.Actions.Any(a => lockedNodeIds.Contains(a.Target.NodeId))))
            {
                _logger.LogDebug("ShortestRouteChooser.GetNotStartedActionGroupAsync: Found charging action group {ActionGroupCorrelationId} for AGV: {AgvCorrelationId}", chargingActionGroup.CorrelationId, agv.CorrelationId);
                return chargingActionGroup;
            }
            if (lockedNodeIds is { Count: > 0 })
            {
                _logger.LogOrderWarning(LogCodes.OrderWithLockedStation, agv.CorrelationId, chargingActionGroup.CorrelationId, "Order Contains Locked Station",
                    $"Charging Order {chargingActionGroup.OrderId} contains locked station.");
            }
        }

        IReadOnlyList<Models.ActionGroup.ActionGroup> notStartedActionGroups = await _actionGroupRepository.GetNotStartedActionGroupsAsync(agv.CorrelationId);
        var manualActionGroup = notStartedActionGroups.OrderByDescending(g => g.HasPriority).FirstOrDefault(g => g.IsManual);
        if (manualActionGroup != null)
        {
            if (lockedNodeIds is { Count: > 0 }  && manualActionGroup.Actions.Any(a => lockedNodeIds.Contains(a.Target.NodeId)))
            {
                _logger.LogOrderWarning(LogCodes.OrderWithLockedStation, agv.CorrelationId, manualActionGroup.CorrelationId, "Order Contains Locked Station",
                    $"Order {manualActionGroup.OrderId} contains locked station.");
            }
            // If there is a manual action group, return this action group. The list of actionGroups is already ordered by the CreationDate, so the first is fine 
            return manualActionGroup;
        }

        // Filter these action group which have actions with locked target.
        IReadOnlyList<Models.ActionGroup.ActionGroup> filteredNextActionGroups = lockedNodeIds is { Count: > 0 }
            ? notStartedActionGroups.Where(g => g.Actions.All(a => !lockedNodeIds.Contains(a.Target.NodeId))).ToList()
            : notStartedActionGroups;

        List<Models.ActionGroup.ActionGroup> priorityActionGroups = filteredNextActionGroups.Where(a => a.HasPriority).ToList();
        var nextActionGroup = await SelectNextActionGroup(agv, agvData, cancellationToken, priorityActionGroups);
        if (nextActionGroup != null)
        {
            return nextActionGroup;
        }
        List<Models.ActionGroup.ActionGroup> nextActionGroups = filteredNextActionGroups.Except(priorityActionGroups).ToList();

        List<Models.ActionGroup.ActionGroup> nextActionGroupsForAgv = nextActionGroups.Where(a => a.AssignedAgv == agv.CorrelationId).ToList();
        nextActionGroup = await SelectNextActionGroup(agv, agvData, cancellationToken, nextActionGroupsForAgv);
        if (nextActionGroup != null)
        {
            return nextActionGroup;
        }
        return await SelectNextActionGroup(agv, agvData, cancellationToken, nextActionGroups.Except(nextActionGroupsForAgv).ToList());
    }

    /// <summary>
    /// Select the next action group.
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <param name="agvData">The agv data.</param>
    /// <param name="cancellationToken">The cancellation token </param>
    /// <param name="notStartedActionGroups">The not started action groups</param>
    /// <returns></returns>
    protected abstract Task<Models.ActionGroup.ActionGroup?> SelectNextActionGroup(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups);
}