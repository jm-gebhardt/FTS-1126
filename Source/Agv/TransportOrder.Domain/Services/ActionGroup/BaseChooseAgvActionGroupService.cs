using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Provides a base class for implementations of <see cref="IChooseAgvActionGroupService"/>
/// </summary>
public abstract class BaseChooseAgvActionGroupService : IChooseAgvActionGroupService
{
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly INodeRepository _nodeRepository;
    private readonly ILogger<BaseChooseAgvActionGroupService> _logger;
    private readonly IQueueRepository _queueRepository;
    private readonly IQueueManager _queueManager;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// The route service
    /// </summary>
    protected readonly IRouteService RouteService;

    /// <inheritdoc cref="IChooseAgvActionGroupService"/>>
    protected BaseChooseAgvActionGroupService(
        IActionGroupRepository actionGroupRepository,
        INodeRepository nodeStateRepository,
        IQueueRepository queueRepository,
        IQueueManager queueManager,
        IRouteService routeService,
        ILogger<BaseChooseAgvActionGroupService> logger,
        ILoggingService loggingService
    )
    {
        _logger = logger;
        _actionGroupRepository = actionGroupRepository;
        _nodeRepository = nodeStateRepository;
        _queueRepository = queueRepository;
        _queueManager = queueManager;
        RouteService = routeService;
        _loggingService = loggingService;
    }

    /// <summary>
    /// Returns the first ActionGroup that satisfies these conditions:
    /// <para>- Is action group assigned to the agv with the highest <see cref="Priority"/>, that is not an automatic pick drop action group.</para>
    /// <para>- Is there an action group with a locked action target node then skip this action group.</para>
    /// <para>- Is there an automatic action group that passes queue nodes then skip this action group.</para>
    /// <para>- Is an action group assigned to the agv selected based on the policy implementing chooser.</para>
    /// <para>- Is an action group with a <see cref="Priority"/> higher than the default selected based on the policy implementing chooser.</para>
    /// <para>- Is an action group selected based on the policy implementing chooser.</para>
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <param name="agvData">The data of the agv.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>ActionGroup</returns>
    /// <exception cref="AgvLocationUnknownException"></exception>
    public async Task<Models.ActionGroup.ActionGroup?> GetNotStartedActionGroupAsync(Agv agv, AgvData? agvData, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Guid>? lockedNodeIds = await _nodeRepository.GetLockedNodes();
        if (lockedNodeIds is { Count: > 0 })
        {
            _logger.LogDebug("Locked nodes: {Ids}", string.Join(",", lockedNodeIds));
        }

        IReadOnlyList<Models.ActionGroup.ActionGroup> notStartedActionGroups = await _actionGroupRepository.GetNotStartedActionGroupsAsync(agv.CorrelationId);

        //1) Action group assigned to the agv with the highest priority, that is not an automatic pick drop action group (e.g. charging, manual, queueing, queue evacuation)
        var nextActionGroup = notStartedActionGroups.FirstOrDefault(a => a.AssignedAgv == agv.CorrelationId && (a.IsManual || !a.IsMovingCargo));
        if (nextActionGroup != null)
        {
            if (lockedNodeIds is { Count: > 0 } && nextActionGroup.Actions.Any(a => lockedNodeIds.Contains(a.Target.NodeId!.Value)))
            {
                await _loggingService.LogOrder(_logger, LogLevel.Warning, LogCodes.OrderWithLockedStation, agv.CorrelationId, nextActionGroup.CorrelationId, "Order Contains Locked Station",
                    $"Order {nextActionGroup.OrderId} contains locked station.",
                    new Dictionary<string, object?> { { LogLabels.OrderId, nextActionGroup.OrderId } });
            }

            if (nextActionGroup.IsManual || lockedNodeIds is { Count: 0 } ||
                nextActionGroup.IsChargingOrder ||
                (lockedNodeIds is { Count: > 0 } && !nextActionGroup.Actions.Any(a => lockedNodeIds.Any(ln => ln == a.Target.Node?.Id))))
            {
                _logger.LogInformation("Selected next assigned action group {ActionGroupCorrelationId} for agv {AgvCorrelationId} starting from node {LastNodeId}", nextActionGroup.CorrelationId, agv.CorrelationId, agv.AgvStatus?.LastNodeId);
                return nextActionGroup;
            }
        }

        if (agv.IsQueueing)
        {
            _logger.LogWarning("Prevent selecting next pick drop action group for queueing agv {AgvCorrelationId}", agv.CorrelationId);
            return null;
        }

        // Filter action group which have actions with locked target.
        IReadOnlyList<Models.ActionGroup.ActionGroup> filteredNextActionGroups = lockedNodeIds is { Count: > 0 } ? notStartedActionGroups.Where(g => g.Actions.All(a => lockedNodeIds.All(ln => ln != a.Target.Node?.Id))).ToList() : notStartedActionGroups;

        //2) Action group assigned to the agv with the highest priority (e.g. Doppelspiel, re-assigned action groups, resumed action groups)
        List<Models.ActionGroup.ActionGroup> assignedActionGroups = filteredNextActionGroups.Where(a => a.AssignedAgv == agv.CorrelationId).ToList();
        if (assignedActionGroups.Count > 0)
        {
            nextActionGroup = await SelectNextActionGroup(agv, agvData, cancellationToken, assignedActionGroups, []);
            if (nextActionGroup != null)
            {
                _logger.LogInformation("Selected next priority action group {ActionGroupCorrelationId} for agv {AgvCorrelationId} starting from node {LastNodeId}", nextActionGroup.CorrelationId, agv.CorrelationId, agv.AgvStatus?.LastNodeId);
                return nextActionGroup;
            }
        }

        //Get nodes that are not allowed to be on the route from the current AGV
        //To be specific: Nodes that part of a queue and route would lead through a queue
        ICollection<Guid> nodesToAvoid = await GetNodesToAvoid(agv.AgvStatus?.LastNodeId);

        //If e.g., lastNodeId from AGV was not correct, we check if AGV is at queue head
        //and remove queue head node explicitly again from nodes to avoid
        if ((await _queueManager.GetAgvsAtQueueHeads()).Contains(agv.CorrelationId))
        {
            var queueAgvIsIn = await _queueRepository.GetQueueForAgv(agv.CorrelationId);
            if (queueAgvIsIn?.Head?.NodeId != null)
            {
                var queueHead = queueAgvIsIn.Head.NodeId;
                nodesToAvoid = nodesToAvoid.Except([queueHead]).ToList();
            }
        }

        //3) Unassigned action groups with non-default priority
        List<Models.ActionGroup.ActionGroup> priorityActionGroups = filteredNextActionGroups.Except(assignedActionGroups).Where(a => a.Priority > Priority.Default).ToList();
        if (priorityActionGroups.Count > 0)
        {
            nextActionGroup = await SelectNextActionGroup(agv, agvData, cancellationToken, priorityActionGroups, nodesToAvoid);
            if (nextActionGroup != null)
            {
                _logger.LogInformation("Selected next priority action group {ActionGroupCorrelationId} for agv {AgvCorrelationId} starting from node {LastNodeId} with nodes to avoid: {AvoidNodes}", nextActionGroup.CorrelationId, agv.CorrelationId, agv.AgvStatus?.LastNodeId, string.Join(":", nodesToAvoid));
                return nextActionGroup;
            }
        }

        //4) Any other action group
        List<Models.ActionGroup.ActionGroup> nextActionGroups = filteredNextActionGroups.Except(priorityActionGroups).ToList();
        nextActionGroup = await SelectNextActionGroup(agv, agvData, cancellationToken, nextActionGroups.ToList(), nodesToAvoid);
        if (nextActionGroup != null)
        {
            _logger.LogInformation("Selected next action group {ActionGroupCorrelationId} for agv {AgvCorrelationId} starting from node {LastNodeId} with nodes to avoid: {AvoidNodes}", nextActionGroup.CorrelationId, agv.CorrelationId, agv.AgvStatus?.LastNodeId, string.Join(":", nodesToAvoid));
        }
        return nextActionGroup;
    }

    /// <summary>
    /// Get the node ids of nodes to avoid.
    /// </summary>
    private async Task<ICollection<Guid>> GetNodesToAvoid(Guid? lastNodeId)
    {
        // Avoid nodes of occupied queues
        ICollection<Guid> queueNodes = await _queueRepository.GetBlockedQueueNodes();
        return lastNodeId == null ? queueNodes : queueNodes.Except([lastNodeId.Value]).ToList();
    }

    /// <summary>
    /// Select the next action group.
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <param name="agvData">The agv data.</param>
    /// <param name="cancellationToken">The cancellation token </param>
    /// <param name="notStartedActionGroups">The not started action groups</param>
    /// <param name="nodesToAvoid">Nodes to avoid.</param>
    /// <returns></returns>
    protected abstract Task<Models.ActionGroup.ActionGroup?> SelectNextActionGroup(Agv agv, AgvData? agvData, CancellationToken cancellationToken, ICollection<Models.ActionGroup.ActionGroup> notStartedActionGroups, ICollection<Guid> nodesToAvoid);
}