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
    /// Tries to find a suitable AGV for this action group
    /// Find AGV with shortest route to pick
    /// </summary>
    public Task<IAgv?> GetAgvAsync(ActionGroup actionGroup, IList<IAgv> agvs)
    {
        IAgv? bestAgv = null;
        if (!agvs.Any())
        {
            // No assignable AGVs available
            return Task.FromResult(bestAgv);
        }

        // Create a list of waypoints from the action targets
        List<string> waypoints = actionGroup.GetRequiredActions().OrderBy(a => a.Sequence).Select(a => a.TargetNodeId).ToList();
        string firstWaypointId = waypoints.First();

        List<KeyValuePair<IAgv, (bool hasRoute, float? costs)>> agvWithRoute = agvs
            .ToDictionary(a => a,
                a =>
                {
                    bool hasRoute = _routeService.HasRoute(a, firstWaypointId, out float? costs);
                    return (hasRoute, costs);
                })
            .Where(cba => cba.Value.hasRoute)
            .ToList();

        if (!agvWithRoute.Any())
        {
            // no of these Agv can handle the route. Will be taken by any busy Agv later
            return Task.FromResult(bestAgv);
        }

        bestAgv = agvWithRoute.MinBy(cba => cba.Value.costs).Key;

        return Task.FromResult((IAgv?) bestAgv);
    }

    /// <summary>
    /// Get not started action groups, returns the first action in the first action group that Agv can handle all actions
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    /// <exception cref="AgvLocationUnknownException"></exception>
    public async Task<AgvAction?> GetActionGroupAsync(IAgv agv)
    {
        _logger.LogInformation("start GetNextParentedByAction for agvId: {AgvId}", agv.AgvId);
        agv.GetRequiredAgvStatus().RequireLastNodeId();

        IReadOnlyList<ActionGroup> notStartedActionGroups = await _actionGroupRegistry.GetNotStartedActionGroupsAsync();
        var nextParentedByAction = (notStartedActionGroups
            .Select(a => a.GetRequiredActions().Select(action =>
            {
                bool hasRoute = _routeService.HasRoute(agv, action.TargetNodeId, out float? costs);
                return (hasRoute, action, costs);
            }))
            .Where(actionGroup => actionGroup.All(al => al.hasRoute))
            // out of the ActionGroup: take action with lowest sequence number = pick
            // take ActionGroup where this pick action has the lowest costs
            .MinBy(actionGroup => actionGroup.MinBy(p => p.action.Sequence).costs)?
            .FirstOrDefault())?.action;

        return nextParentedByAction;
    }
}