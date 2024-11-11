using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Choose based and shortest route, if the action group is not older than 2 min.
/// </summary>
public class FastestRouteAndThresholdChooser : BaseChooseAgvActionGroupService
{
    private readonly IRouteService _routeService;
    private readonly int _orderWaitingTimeThreshold;

    /// <inheritdoc cref="ShortestRouteChooser"/>>
    public FastestRouteAndThresholdChooser(
        IRouteService routeService,
        IActionGroupRepository actionGroupRepository,
        INodeStateRepository nodeStateRepository,
        IQueueRepository queueRepository,
        ILogger<FastestRouteAndThresholdChooser> logger
    ) : base(actionGroupRepository, nodeStateRepository, queueRepository, logger)
    {
        _routeService = routeService;
        _orderWaitingTimeThreshold = 2;
    }

    /// <inheritdoc cref="BaseChooseAgvActionGroupService"/>>
    protected override async Task<Models.ActionGroup.ActionGroup?> SelectNextActionGroup(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups, ICollection<string> nodesToAvoid)
    {
        var now = DateTime.UtcNow;
        IOrderedEnumerable<Models.ActionGroup.ActionGroup> priorityGroups = notStartedActionGroups.Where(a => ( now - a.CreatedDate).TotalMinutes > _orderWaitingTimeThreshold).OrderBy(a => a.CreatedDate);

        foreach (var priorityGroup in priorityGroups)
        {
            double? cost = await _routeService.GetCostsForFirstActionNotPassingNodesAsync(agv, priorityGroup, agvData, nodesToAvoid, cancellationToken);
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
            double? firstActionCost = await _routeService.GetCostsForFirstActionNotPassingNodesAsync(agv, actionGroup, agvData, nodesToAvoid, cancellationToken);

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