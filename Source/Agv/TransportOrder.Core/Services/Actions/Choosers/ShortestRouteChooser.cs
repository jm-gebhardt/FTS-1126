using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Extensions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Choosers;

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
    /// Checks if given AGV has route for given ActionGroup
    /// firstAction contains the first action of the ActionGroup or null if no route is found
    /// firstActionCost contains the costs for the first action of the ActionGroup or float.MaxValue if no route is found
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="actionGroup"></param>
    /// <param name="firstActionCost"></param>
    /// <returns></returns>
    private bool HasRoute(IAgv agv, ActionGroup actionGroup, out float firstActionCost)
    {
        AgvAction? firstAction = null;
        firstActionCost = float.MaxValue;

        IOrderedEnumerable<AgvAction> actions = actionGroup
            .GetRequiredActions() // Throws exception if there are no actions
            .OrderBy(a => a.Sequence);
        
        foreach (var action in actions)
        {
            if (!_routeService.HasRoute(agv, action.TargetNodeId, out float? costs))
            {
                return false; // Break if a target node is not reachable
            }

            // keep first action and it's costs
            if (firstAction == null)
            {
                firstAction = action;
                firstActionCost = costs.GetValueOrDefault();
            }
        }

        return true;
    }

    /// <summary>
    /// Tries to find a AGV with shortest route to the pick action with the condition, that all actions are reachable
    /// </summary>
    public Task<IAgv?> GetAgvAsync(ActionGroup actionGroup, IList<IAgv> agvs)
    {
        IAgv? bestAgv = null;
        float minCosts = float.MaxValue;
        foreach (var agv in agvs)
        {
            if (!HasRoute(agv, actionGroup, out float firstActionCost) || firstActionCost > minCosts)
            {
                continue;
            }
            minCosts = firstActionCost;
            bestAgv = agv;
        }

        return Task.FromResult(bestAgv);
    }

    /// <summary>
    /// Returns the first action of an ActionGroup that satisfies these conditions:
    /// <para>- A Route for all actions exists</para>
    /// <para>- The cost of the first action is the lowest</para>
    /// </summary>
    /// <param name="agv"></param>
    /// <returns>AgvAction</returns>
    /// <exception cref="AgvLocationUnknownException"></exception>
    public async Task<ActionGroup?> GetNotStartedActionGroupAsync(IAgv agv)
    {
        agv.GetRequiredAgvStatus().RequireLastNodeId();

        IReadOnlyList<ActionGroup> notStartedActionGroups = await _actionGroupRegistry.GetNotStartedActionGroupsAsync(agv.CorrelationId);

        var manualActionGroup = notStartedActionGroups.FirstOrDefault(g => g.IsManual);

        if (manualActionGroup != null)
        {
            // if there is a manual action group, return the action. The list of actionGroups is already ordered by the CreationDate, so the first is fine 
            return manualActionGroup;
        }

        float minCosts = float.MaxValue;
        
        ActionGroup? nextActionGroup = null;
        foreach (var actionGroup in notStartedActionGroups)
        {
            if (!HasRoute(agv, actionGroup, out float firstActionCost) || firstActionCost > minCosts)
            {
                continue;
            }
            minCosts = firstActionCost;
            nextActionGroup = actionGroup;
        }

        return nextActionGroup;
    }
}