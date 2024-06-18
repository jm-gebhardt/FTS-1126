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
    /// Tries to find a AGV with shortest route to the pick action with the condition, that all actions are reachable
    /// </summary>
    public async Task<Agv?> GetAgvAsync(Models.ActionGroup.ActionGroup actionGroup, IList<Agv> agvs, IList<AgvData> agvData, CancellationToken cancellationToken = default)
    {
        Agv? bestAgv = null;
        double minCosts = double.MaxValue;
        foreach (var agv in agvs)
        {
            if (await _actionGroupRepository.HasUnfinishedDropActionsAsync(agv.CorrelationId) || await _actionGroupRepository.AgvHasPriorityActionGroups(agv.CorrelationId))
            {
                continue;
            }
            try
            {
                double? firstActionCost = await _routeService.GetCostsForFirstActionAsync(agv, actionGroup, agvData.SingleOrDefault(d => d.CorrelationId == agv.CorrelationId), cancellationToken);
                if (firstActionCost == null || firstActionCost.GetValueOrDefault(float.MaxValue) > minCosts)
                {
                    continue;
                }

                minCosts = firstActionCost.Value;
                bestAgv = agv;
            }
            catch (AgvLocationUnknownException exception)
            {
                _logger.LogError(exception, "{message}", exception.Message);
            }
        }

        return bestAgv;
    }

    /// <summary>
    /// Returns the first action of an ActionGroup that satisfies these conditions:
    /// <para>- A Route for all actions exists</para>
    /// <para>- The cost of the first action is the lowest</para>
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="agvData"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>AgvAction</returns>
    /// <exception cref="AgvLocationUnknownException"></exception>
    public async Task<Models.ActionGroup.ActionGroup?> GetNotStartedActionGroupAsync(Agv agv, AgvData? agvData, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Models.ActionGroup.ActionGroup> notStartedActionGroups = await _actionGroupRepository.GetNotStartedActionGroupsAsync(agv.CorrelationId);

        var manualActionGroup = notStartedActionGroups.OrderByDescending(g => g.HasPriority).FirstOrDefault(g => g.IsManual);

        if (manualActionGroup != null)
        {
            // if there is a manual action group, return the action. The list of actionGroups is already ordered by the CreationDate, so the first is fine 
            return manualActionGroup;
        }

        List<Models.ActionGroup.ActionGroup> priorityActionGroups = notStartedActionGroups.Where(a => a.HasPriority).ToList();
        var nextActionGroup = await SelectNextActionGroup(agv, agvData, cancellationToken, priorityActionGroups);
        if (nextActionGroup != null)
        {
            return nextActionGroup;
        }

        return await SelectNextActionGroup(agv, agvData, cancellationToken, notStartedActionGroups.Except(priorityActionGroups).ToList());
    }

    private async Task<Models.ActionGroup.ActionGroup?> SelectNextActionGroup(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups)
    {
        double minCosts = double.MaxValue;

        Models.ActionGroup.ActionGroup? nextActionGroup = null;
        foreach (var actionGroup in notStartedActionGroups)
        {
            double? firstActionCost = await _routeService.GetCostsForFirstActionAsync(agv, actionGroup, agvData, cancellationToken);

            if (firstActionCost == null || firstActionCost.GetValueOrDefault(float.MaxValue) > minCosts || actionGroup.FirstNotStartedAction!.IsIncomplete)
            {
                continue;
            }

            minCosts = firstActionCost.Value;

            nextActionGroup = actionGroup;
        }

        return nextActionGroup;
    }
}