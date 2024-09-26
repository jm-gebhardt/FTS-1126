using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Choose alternating between optimization based on the oldest action group and shortest route.
/// </summary>
public class AlternatingModeChooser : BaseChooseAgvActionGroupService
{
    private readonly IRouteService _routeService;

    /// <summary>
    /// Selection mode is fastest route.
    /// </summary>
    public bool FastestRouteMode;

    /// <inheritdoc cref="AlternatingModeChooser"/>>
    public AlternatingModeChooser(
        IRouteService routeService,
        IActionGroupRepository actionGroupRepository,
        INodeStateRepository nodeStateRepository,
        ILogger<AlternatingModeChooser> logger
    ) : base(actionGroupRepository, nodeStateRepository, logger)
    {
        _routeService = routeService;
    }

    /// <inheritdoc cref="BaseChooseAgvActionGroupService"/>>
    protected override async Task<Models.ActionGroup.ActionGroup?> SelectNextActionGroup(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups)
    {
        FastestRouteMode = !FastestRouteMode;
        if (FastestRouteMode)
        {
            return await SelectBasedOnShortestRoute(agv, agvData, cancellationToken, notStartedActionGroups);
        }
        return notStartedActionGroups.OrderBy(a => a.CreatedDate).FirstOrDefault();
    }

    private async Task<Models.ActionGroup.ActionGroup?> SelectBasedOnShortestRoute(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups)
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