using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Choose the shortest route of x oldest action groups
/// </summary>
public class FastestRouteOfXOldestActionGroupsChooser : BaseChooseAgvActionGroupService
{
    private readonly int _numberOfOrders;

    /// <inheritdoc cref="ShortestRouteChooser"/>>
    public FastestRouteOfXOldestActionGroupsChooser(
        IRouteService routeService,
        IActionGroupRepository actionGroupRepository,
        INodeRepository nodeRepository,
        IQueueRepository queueRepository,
        IQueueManager queueManager,
        ILogger<FastestRouteOfXOldestActionGroupsChooser> logger,
        ILoggingService loggingService
    ) : base(actionGroupRepository, nodeRepository, queueRepository, queueManager, routeService, logger, loggingService)
    {
        _numberOfOrders = 2;
    }

    /// <inheritdoc cref="BaseChooseAgvActionGroupService"/>>
    protected override async Task<Models.ActionGroup.ActionGroup?> SelectNextActionGroup(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups, ICollection<Guid> nodesToAvoid)
    {
        double minCosts = double.MaxValue;

        Models.ActionGroup.ActionGroup? nextActionGroup = null;
        foreach (var actionGroup in notStartedActionGroups.OrderBy(g => g.CreatedDate).Take(_numberOfOrders))
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
}