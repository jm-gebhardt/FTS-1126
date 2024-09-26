using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Uses route cost as base score and scales it with the acion group waiting time
/// </summary>
public class DynamicPriorisationChooser : BaseChooseAgvActionGroupService
{
    private readonly IRouteService _routeService;

    /// <inheritdoc cref="ShortestRouteChooser"/>>
    public DynamicPriorisationChooser(
        IRouteService routeService,
        IActionGroupRepository actionGroupRepository,
        INodeStateRepository nodeStateRepository,
        ILogger<DynamicPriorisationChooser> logger
    ) : base(actionGroupRepository, nodeStateRepository, logger)
    {
        _routeService = routeService;
    }

    /// <inheritdoc cref="BaseChooseAgvActionGroupService"/>>
    protected override async Task<Models.ActionGroup.ActionGroup?> SelectNextActionGroup(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups)
    {
        foreach (var actionGroup in notStartedActionGroups.OrderBy(g => g.CreatedDate))
        {
            if (actionGroup.FirstNotStartedAction!.IsIncomplete)
            {
                continue;
            }
            double? firstActionCost = await _routeService.GetCostsForFirstActionAsync(agv, actionGroup, agvData, cancellationToken);
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