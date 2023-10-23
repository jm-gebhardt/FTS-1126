using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Simple logic based on the optimization of shortest route
/// </summary>
public class ShortestRouteChooser : IChooseAgvActionGroupService
{
    private readonly IRouteService _routeService;
    private readonly IActionGroupRegistry _actionGroupRegistry;
    private readonly ILogger<ShortestRouteChooser> _logger;

    /// <inheritdoc cref="ShortestRouteChooser"/>>
    public ShortestRouteChooser(
        IRouteService routeService,
        IActionGroupRegistry actionGroupRegistry,
        ILogger<ShortestRouteChooser> logger
    )
    {
        _routeService = routeService;
        _logger = logger;
        _actionGroupRegistry = actionGroupRegistry;
    }

    /// <summary>
    /// Tries to find a AGV with shortest route to the pick action with the condition, that all actions are reachable
    /// </summary>
    public async Task<Models.Agv.Agv?> GetAgvAsync(Models.ActionGroup.ActionGroup actionGroup, IList<Models.Agv.Agv> agvs, CancellationToken cancellationToken = default)
    {
        Models.Agv.Agv? bestAgv = null;
        float minCosts = float.MaxValue;
        foreach (var agv in agvs)
        {
            try
            {
                float? firstActionCost = await _routeService.GetCostsForFirstActionAsync(agv, actionGroup, cancellationToken);
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
    /// <param name="cancellationToken"></param>
    /// <returns>AgvAction</returns>
    /// <exception cref="AgvLocationUnknownException"></exception>
    public async Task<Models.ActionGroup.ActionGroup?> GetNotStartedActionGroupAsync(Models.Agv.Agv agv, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Models.ActionGroup.ActionGroup> notStartedActionGroups = await _actionGroupRegistry.GetNotStartedActionGroupsAsync(agv.CorrelationId);

        var manualActionGroup = notStartedActionGroups.FirstOrDefault(g => g.IsManual);

        if (manualActionGroup != null)
        {
            // if there is a manual action group, return the action. The list of actionGroups is already ordered by the CreationDate, so the first is fine 
            return manualActionGroup;
        }

        float minCosts = float.MaxValue;

        Models.ActionGroup.ActionGroup? nextActionGroup = null;
        foreach (var actionGroup in notStartedActionGroups)
        {
            float? firstActionCost = await _routeService.GetCostsForFirstActionAsync(agv, actionGroup, cancellationToken);

            if (firstActionCost == null || firstActionCost.GetValueOrDefault(float.MaxValue) > minCosts)
            {
                continue;
            }

            minCosts = firstActionCost.Value;

            nextActionGroup = actionGroup;
        }

        return nextActionGroup;
    }
}