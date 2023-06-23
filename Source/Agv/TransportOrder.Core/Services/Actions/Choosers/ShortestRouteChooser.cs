using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Registries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Choosers;

/// <summary>
/// Simple logic based on the optimization of shortest route
/// </summary>
public class ShortestRouteChooser : IChooseAgvActionGroupService
{
    private readonly IRouteService _routeService;
    private readonly IActionGroupRegistry _actionGroupRegistry;
    private readonly ILogger<ShortestRouteChooser> _logger;

    /// <inheritdoc cref="ShortestRouteChooser"/>>
    /// <param name="routeService"></param>
    /// <param name="actionGroupRegistry"></param>
    /// <param name="logger"></param>
    public ShortestRouteChooser(
        IRouteService routeService,
        IActionGroupRegistry actionGroupRegistry,
        ILogger<ShortestRouteChooser> logger
    )
    {
        _routeService = routeService;
        _actionGroupRegistry = actionGroupRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Tries to find a AGV with shortest route to the pick action with the condition, that all actions are reachable
    /// </summary>
    public Task<IAgv?> GetAgvAsync(ActionGroup actionGroup, IList<IAgv> agvs)
    {
        IAgv? bestAgv = null;
        if (!agvs.Any()) // No assignable AGVs available
        {
            return Task.FromResult(bestAgv);
        }

        float? minCosts = null;
        foreach (var agv in agvs)
        {
            bool hasRoute = HasRoute(agv, actionGroup, out var firstAction, out float? firstActionCost);
            if (!hasRoute || firstAction == null || firstActionCost == null)
            {
                continue; // Not all actions of this actionGroup can be reached by the AGV -> skip
            }

            // Find actionGroup with the lowest first node cost
            if (minCosts == null || firstActionCost < minCosts)
            {
                bestAgv = agv;
                minCosts = firstActionCost;
            }
        }

        return Task.FromResult(bestAgv);
    }

    /// <summary>
    /// Returns the first action of an actiongroup that satisfies these conditions:
    /// <para>- A Route for all actions exists</para>
    /// <para>- The cost of the first action is the lowest</para>
    /// </summary>
    /// <param name="agv"></param>
    /// <returns>AgvAction</returns>
    /// <exception cref="AgvLocationUnknownException"></exception>
    public async Task<AgvAction?> GetActionGroupAsync(IAgv agv)
    {
        _logger.LogInformation("start GetNextParentedByAction for agvId: {AgvId}", agv.AgvId);
        agv.GetRequiredAgvStatus().RequireLastNodeId();

        IReadOnlyList<ActionGroup> notStartedActionGroups = await _actionGroupRegistry.GetNotStartedActionGroupsAsync();

        float? minCosts = null;
        AgvAction? nextParentedByAction = null;
        foreach (var actionGroup in notStartedActionGroups)
        {
            bool hasRoute = HasRoute(agv, actionGroup, out var firstAction, out float? firstActionCost);
            if (!hasRoute || firstAction == null || firstActionCost == null)
            {
                continue; // Not all actions of this actionGroup can be reached by the AGV -> skip
            }

            // Find actionGroup with the lowest first node cost
            if (minCosts == null || firstActionCost < minCosts)
            {
                nextParentedByAction = firstAction;
                minCosts = firstActionCost;
            }
        }

        return nextParentedByAction;
    }

    private bool HasRoute(IAgv agv, ActionGroup actionGroup, out AgvAction? firstAction, out float? firstActionCost)
    {
        firstAction = null;
        firstActionCost = null;

        int minSequence = 0;
        ICollection<AgvAction> actions = actionGroup.GetRequiredActions(); // Throws exception if there are no actions
        foreach (var action in actions)
        {
            if (!_routeService.HasRoute(agv, action.TargetNodeId, out float? costs))
            {
                return false; // Break if a target node is not reachable
            }

            // Get action with lowest sequence (this should always be the first action)
            if (firstAction == null || action.Sequence < minSequence)
            {
                firstAction = action;
                firstActionCost = costs;
                minSequence = action.Sequence;
            }
        }

        return true;
    }
}