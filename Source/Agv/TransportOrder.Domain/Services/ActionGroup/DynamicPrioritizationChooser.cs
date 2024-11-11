using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Uses route cost as base score and scales it with the action group waiting time
/// </summary>
public class DynamicPrioritizationChooser : BaseChooseAgvActionGroupService
{
    private readonly IRouteService _routeService;

    /// <inheritdoc cref="ShortestRouteChooser"/>>
    public DynamicPrioritizationChooser(
        IRouteService routeService,
        IActionGroupRepository actionGroupRepository,
        INodeStateRepository nodeStateRepository,
        IQueueRepository queueRepository,
        ILogger<DynamicPrioritizationChooser> logger
    ) : base(actionGroupRepository, nodeStateRepository, queueRepository,  logger)
    {
        _routeService = routeService;
    }

    /// <inheritdoc cref="BaseChooseAgvActionGroupService"/>>
    protected override async Task<Models.ActionGroup.ActionGroup?> SelectNextActionGroup(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups, ICollection<string> nodesToAvoid)
    {
        foreach (var actionGroup in notStartedActionGroups.OrderBy(g => g.CreatedDate))
        {
            if (actionGroup.FirstNotStartedAction!.IsIncomplete)
            {
                continue;
            }
            double? firstActionCost = await _routeService.GetCostsForFirstActionNotPassingNodesAsync(agv, actionGroup, agvData, nodesToAvoid, cancellationToken);
            if (firstActionCost == null)
            {
                continue;
            }
            actionGroup.EstimatedDuration = TimeSpan.FromSeconds(firstActionCost.Value);
        }
        var now = DateTime.UtcNow;
        IOrderedEnumerable<Models.ActionGroup.ActionGroup> sortedGroups = notStartedActionGroups
            .Where(a => a.EstimatedDuration != null).OrderBy(o =>
                o.EstimatedDuration!.Value.TotalSeconds / (1 + Math.Pow((now - o.CreatedDate).TotalMinutes, 2)));
        return sortedGroups.FirstOrDefault();
    }
}