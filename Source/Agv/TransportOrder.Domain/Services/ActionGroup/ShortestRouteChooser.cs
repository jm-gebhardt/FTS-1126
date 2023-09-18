using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Simple logic based on the optimization of shortest route
/// </summary>
public class ShortestRouteChooser : IChooseAgvActionGroupService
{
    private readonly IRouteService _routeService;
    private readonly IActionGroupRegistry _actionGroupRegistry;

    /// <inheritdoc cref="ShortestRouteChooser"/>>
    /// <param name="routeService"></param>
    /// <param name="actionGroupRegistry"></param>
    public ShortestRouteChooser(
        IRouteService routeService,
        IActionGroupRegistry actionGroupRegistry
    )
    {
        _routeService = routeService;
        _actionGroupRegistry = actionGroupRegistry;
    }

    /// <summary>
    /// Tries to find a AGV with shortest route to the pick action with the condition, that all actions are reachable
    /// </summary>
    public async Task<IAgv?> GetAgvAsync(Models.ActionGroup.ActionGroup actionGroup, IList<IAgv> agvs, CancellationToken cancellationToken = default)
    {
        IAgv? bestAgv = null;
        float minCosts = float.MaxValue;
        foreach (var agv in agvs)
        {
            float? firstActionCost = await _routeService.GetCostsForFirstActionAsync(agv, actionGroup, cancellationToken);
            if (firstActionCost == null || firstActionCost.GetValueOrDefault(float.MaxValue) > minCosts)
            {
                continue;
            }

            minCosts = firstActionCost.Value;
            bestAgv = agv;
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
    public async Task<Models.ActionGroup.ActionGroup?> GetNotStartedActionGroupAsync(IAgv agv, CancellationToken cancellationToken = default)
    {
        agv.GetRequiredAgvStatus().RequireLastNodeId();

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