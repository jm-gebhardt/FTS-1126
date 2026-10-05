using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Choose based and shortest route, if the action group is not older than 2 min.
/// </summary>
public class FastestRouteAndThresholdChooser : BaseChooseAgvActionGroupService
{
    private readonly int _orderWaitingTimeThreshold;

    /// <inheritdoc cref="ShortestRouteChooser"/>>
    public FastestRouteAndThresholdChooser(
        IRouteService routeService,
        IActionGroupRepository actionGroupRepository,
        INodeRepository nodeRepository,
        IQueueRepository queueRepository,
        IQueueManager queueManager,
        ILogger<FastestRouteAndThresholdChooser> logger,
        ILoggingService loggingService
    ) : base(actionGroupRepository, nodeRepository, queueRepository, queueManager, routeService, logger, loggingService)
    {
        _orderWaitingTimeThreshold = 2;
    }

    /// <inheritdoc cref="BaseChooseAgvActionGroupService"/>>
    protected override async Task<Models.ActionGroup.ActionGroup?> SelectNextActionGroup(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups, ICollection<Guid> nodesToAvoid)
    {
        var now = DateTime.UtcNow;
        var priorityGroups = notStartedActionGroups.Where(a => ( now - a.CreatedDate).TotalMinutes > _orderWaitingTimeThreshold).OrderBy(a => a.CreatedDate);

        foreach (var priorityGroup in priorityGroups)
        {
            double? cost = await RouteService.GetCostsForFirstActionNotPassingNodesAsync(agv, priorityGroup, agvData, nodesToAvoid, cancellationToken);
            if (cost != null)
            {
                return priorityGroup;
            }
        }
       
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
}