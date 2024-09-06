using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Simple logic based on the optimization of shortest route
/// </summary>
public class ShortestRouteChooser : IChooseAgvActionGroupService
{
    private readonly IRouteService _routeService;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly ILogger<ShortestRouteChooser> _logger;

    /// <inheritdoc cref="ShortestRouteChooser"/>>
    public ShortestRouteChooser(
        IRouteService routeService,
        IActionGroupRepository actionGroupRepository,
        ILogger<ShortestRouteChooser> logger
    )
    {
        _routeService = routeService;
        _logger = logger;
        _actionGroupRepository = actionGroupRepository;
    }

    /// <summary>
    /// Returns the first ActionGroup that satisfies these conditions:
    /// <para>- Is a charging action group</para>
    /// <para>- Is a manual action group</para>
    /// <para>- Is an action group assigned to the agv with an existing route for all actions and the cost of the first action is the lowest.</para>
    /// <para>- Is an action group with an existing route for all actions and the cost of the first action is the lowest.</para>
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <param name="agvData">The data of the agv.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>ActionGroup</returns>
    /// <exception cref="AgvLocationUnknownException"></exception>
    public async Task<Models.ActionGroup.ActionGroup?> GetNotStartedActionGroupAsync(Agv agv, AgvData? agvData, CancellationToken cancellationToken = default)
    {
        var chargingActionGroup = await _actionGroupRepository.GetCreatedChargingActionGroupForAgvAsync(agv.CorrelationId);
        if (chargingActionGroup != null)
        {
            _logger.LogDebug("ShortestRouteChooser.GetNotStartedActionGroupAsync: Found charging action group {ActionGroupCorrelationId} for AGV: {AgvCorrelationId}",chargingActionGroup.CorrelationId, agv.CorrelationId);
            return chargingActionGroup;
        }

        IReadOnlyList<Models.ActionGroup.ActionGroup> notStartedActionGroups = await _actionGroupRepository.GetNotStartedActionGroupsAsync(agv.CorrelationId);
        var manualActionGroup = notStartedActionGroups.OrderByDescending(g => g.HasPriority).FirstOrDefault(g => g.IsManual);
        if (manualActionGroup != null)
        {
            // If there is a manual action group, return this action group. The list of actionGroups is already ordered by the CreationDate, so the first is fine 
            return manualActionGroup;
        }

        List<Models.ActionGroup.ActionGroup> priorityActionGroups = notStartedActionGroups.Where(a => a.HasPriority).ToList();
        var nextActionGroup = await SelectNextActionGroup(agv, agvData, cancellationToken, priorityActionGroups);
        if (nextActionGroup != null)
        {
            return nextActionGroup;
        }
        List<Models.ActionGroup.ActionGroup> nextActionGroups = notStartedActionGroups.Except(priorityActionGroups).ToList();
        List<Models.ActionGroup.ActionGroup> nextActionGroupsForAgv = nextActionGroups.Where(a => a.AssignedAgv == agv.CorrelationId).ToList();
        nextActionGroup = await SelectNextActionGroup(agv, agvData, cancellationToken, nextActionGroupsForAgv);
        if (nextActionGroup != null)
        {
            return nextActionGroup;
        }
        return await SelectNextActionGroup(agv, agvData, cancellationToken, nextActionGroups.Except(nextActionGroupsForAgv).ToList());
    }

    private async Task<Models.ActionGroup.ActionGroup?> SelectNextActionGroup(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups)
    {
        double minCosts = double.MaxValue;

        Models.ActionGroup.ActionGroup? nextActionGroup = null;
        foreach (var actionGroup in notStartedActionGroups.OrderBy(g => g.CreatedDate))
        {
            if (actionGroup.FirstNotStartedAction!.IsIncomplete)
            {
                continue;
            }
            double? firstActionCost = await _routeService.GetCostsForFirstActionAsync(agv, actionGroup, agvData, cancellationToken);

            if (firstActionCost == null || firstActionCost.GetValueOrDefault(float.MaxValue) >= minCosts || actionGroup.FirstNotStartedAction!.IsIncomplete)
            {
                continue;
            }

            minCosts = firstActionCost.Value;

            nextActionGroup = actionGroup;
        }

        return nextActionGroup;
    }
}