using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Selects next action based on which action group is the oldest one.
/// </summary>
public class OldestActionGroupChooser: BaseChooseAgvActionGroupService
{
    /// <inheritdoc cref="OldestActionGroupChooser"/>>
    public OldestActionGroupChooser(
        IRouteService routeService,
        IActionGroupRepository actionGroupRepository,
        INodeRepository nodeRepository,
        IQueueRepository queueRepository,
        IQueueManager queueManager,
        ILogger<OldestActionGroupChooser> logger,
        ILoggingService loggingService
    ) : base(actionGroupRepository, nodeRepository, queueRepository,queueManager, routeService, logger, loggingService)
    {
    }

    /// <inheritdoc cref="BaseChooseAgvActionGroupService"/>>
    protected override async Task<Models.ActionGroup.ActionGroup?> SelectNextActionGroup(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups, ICollection<Guid> nodesToAvoid)
    {
        Models.ActionGroup.ActionGroup? nextActionGroup = null;
        foreach (var actionGroup in notStartedActionGroups.OrderBy(g => g.CreatedDate))
        {
            if (actionGroup.FirstNotStartedAction!.IsIncomplete)
            {
                continue;
            }
            double? firstActionCost = await RouteService.GetCostsForFirstActionNotPassingNodesAsync(agv, actionGroup, agvData, nodesToAvoid, cancellationToken);

            if (firstActionCost == null)
            {
                continue;
            }

            nextActionGroup = actionGroup;
            break;
        }

        return nextActionGroup;
    }
}