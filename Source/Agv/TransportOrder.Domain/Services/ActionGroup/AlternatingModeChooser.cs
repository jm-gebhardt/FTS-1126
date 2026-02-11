using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Choose alternating between optimization based on the oldest action group and shortest route.
/// </summary>
public class AlternatingModeChooser : BaseChooseAgvActionGroupService
{
    /// <summary>
    /// Selection mode is fastest route.
    /// </summary>
    [PublicAPI]
    public bool FastestRouteMode;

    /// <inheritdoc cref="AlternatingModeChooser"/>>
    public AlternatingModeChooser(
        IRouteService routeService,
        IActionGroupRepository actionGroupRepository,
        INodeRepository nodeRepository,
        IQueueRepository queueRepository,
        IQueueManager queueManager,
        ILogger<AlternatingModeChooser> logger,
        ILoggingService loggingService
    ) : base(actionGroupRepository, nodeRepository, queueRepository, queueManager, routeService, logger, loggingService)
    {
    }

    /// <inheritdoc cref="BaseChooseAgvActionGroupService"/>>
    protected override async Task<Models.ActionGroup.ActionGroup?> SelectNextActionGroup(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups, ICollection<Guid> nodesToAvoid)
    {
        FastestRouteMode = !FastestRouteMode;
        if (FastestRouteMode)
        {
            return await SelectBasedOnShortestRoute(agv, agvData, cancellationToken, notStartedActionGroups, nodesToAvoid);
        }
        return await SelectBasedOnOldestRoute(agv, agvData, cancellationToken, notStartedActionGroups, nodesToAvoid);
    }

    private async Task<Models.ActionGroup.ActionGroup?> SelectBasedOnShortestRoute(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups, ICollection<Guid> nodesToAvoid)
    {
        double minCosts = double.MaxValue;

        Models.ActionGroup.ActionGroup? nextActionGroup = null;
        foreach (var actionGroup in notStartedActionGroups.OrderBy(g => g.CreatedDate))
        {
            if (actionGroup.FirstNotStartedAction!.IsIncomplete)
            {
                continue;
            }
            double? firstActionCost = await RouteService.GetCostsForFirstActionNotPassingNodesAsync(agv, actionGroup, agvData, nodesToAvoid, cancellationToken);

            if (firstActionCost == null || firstActionCost.GetValueOrDefault(float.MaxValue) >= minCosts || actionGroup.FirstNotStartedAction!.IsIncomplete)
            {
                continue;
            }

            minCosts = firstActionCost.Value;

            nextActionGroup = actionGroup;
        }

        return nextActionGroup;
    }

    private async Task<Models.ActionGroup.ActionGroup?> SelectBasedOnOldestRoute(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups, ICollection<Guid> nodesToAvoid)
    {
        Models.ActionGroup.ActionGroup? nextActionGroup = null;
        foreach (var actionGroup in notStartedActionGroups.OrderBy(g => g.CreatedDate))
        {
            if (actionGroup.FirstNotStartedAction!.IsIncomplete)
            {
                continue;
            }
            double? firstActionCost = await RouteService.GetCostsForFirstActionNotPassingNodesAsync(agv, actionGroup, agvData, nodesToAvoid, cancellationToken);

            if (firstActionCost == null  || actionGroup.FirstNotStartedAction!.IsIncomplete)
            {
                continue;
            }

            nextActionGroup = actionGroup;
            break;
        }

        return nextActionGroup;
    }
}