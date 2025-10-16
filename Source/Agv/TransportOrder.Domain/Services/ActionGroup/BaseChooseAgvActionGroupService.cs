using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Provides a base class for implementations of <see cref="IChooseAgvActionGroupService"/>
/// </summary>
public abstract class BaseChooseAgvActionGroupService : IChooseAgvActionGroupService
{
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly INodeStateRepository _nodeStateRepository;
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
        INodeStateRepository nodeStateRepository,
        IQueueRepository queueRepository,
        IQueueManager queueManager,
        IRouteService routeService,
        ILogger<BaseChooseAgvActionGroupService> logger,
        ILoggingService loggingService
    )
    {
        _logger = logger;
        _actionGroupRepository = actionGroupRepository;
        _nodeStateRepository = nodeStateRepository;
        _queueRepository = queueRepository;
        _queueManager = queueManager;
        RouteService = routeService;
        _loggingService = loggingService;
    }

    /// <summary>
    /// Returns the first ActionGroup that satisfies these conditions:
    /// <para>- Is a charging action group (with no locked charging station).</para>
    /// <para>- Is a manual action group</para>
    /// <para>- Is there an action group with a locked action target node then skip this action group.</para>
    /// <para>- Is an action group assigned to the agv selected based on the policy implementing chooser.</para>
    /// <para>- Is an action group selected based on the policy implementing chooser.</para>
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <param name="agvData">The data of the agv.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>ActionGroup</returns>
    /// <exception cref="AgvLocationUnknownException"></exception>
    public async Task<Models.ActionGroup.ActionGroup?> GetNotStartedActionGroupAsync(Agv agv, AgvData? agvData, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Guid>? lockedNodeIds = await _nodeStateRepository.GetLockedNodes();
        if (lockedNodeIds is { Count: > 0 })
        {
            _logger.LogDebug("Locked nodes: {Ids}", string.Join(",", lockedNodeIds));
        }

        var chargingActionGroup = await _actionGroupRepository.GetCreatedChargingActionGroupForAgvAsync(agv.CorrelationId);
        if (chargingActionGroup != null)
        {
            if (chargingActionGroup.IsManual || lockedNodeIds is { Count: 0 } ||
                (lockedNodeIds is { Count: > 0 } && !chargingActionGroup.Actions.Any(a => lockedNodeIds.Any(ln => ln == a.Target.Node?.Id))))
            {
                _logger.LogDebug("ShortestRouteChooser.GetNotStartedActionGroupAsync: Found charging action group {ActionGroupCorrelationId} for AGV: {AgvCorrelationId}", chargingActionGroup.CorrelationId, agv.CorrelationId);
                return chargingActionGroup;
            }
            if (lockedNodeIds is { Count: > 0 })
            {
                await _loggingService.LogOrder(_logger, LogLevel.Warning, LogCodes.OrderWithLockedStation, agv.CorrelationId,
                    chargingActionGroup.CorrelationId, "Order Contains Locked Station",
                    $"Order {chargingActionGroup.OrderId} contains locked station.",
                    new Dictionary<string, object?> { { LogLabels.OrderId, chargingActionGroup.OrderId } });
            }
        }

        IReadOnlyList<Models.ActionGroup.ActionGroup> notStartedActionGroups = await _actionGroupRepository.GetNotStartedActionGroupsAsync(agv.CorrelationId);

        var priorityActionGroup = notStartedActionGroups.FirstOrDefault(g => g.HasPriority);
        var manualActionGroup = notStartedActionGroups.OrderByDescending(g => g.HasPriority).FirstOrDefault(g => g.IsManual);
        var chosenActionGroup = manualActionGroup;
        if (priorityActionGroup != null && string.Equals(Queue.EvacuationOrderId, manualActionGroup?.OrderId))
        {
            // Let agv drive to queue head first, if there is an evacuation move
            chosenActionGroup = priorityActionGroup;
        }
        if (chosenActionGroup != null)
        {
            if (lockedNodeIds is { Count: > 0 } && chosenActionGroup.Actions.Any(a => lockedNodeIds.Contains(a.Target.Node!.Id)))
            {
                await _loggingService.LogOrder(_logger, LogLevel.Warning, LogCodes.OrderWithLockedStation, agv.CorrelationId, chosenActionGroup.CorrelationId, "Order Contains Locked Station",
                    $"Order {chosenActionGroup.OrderId} contains locked station.",
                    new Dictionary<string, object?> { { LogLabels.OrderId, chosenActionGroup.OrderId } });
            }
            // If there is a manual or priority action group, return this action group. The list of actionGroups is already ordered by the CreationDate, so the first is fine 
            return chosenActionGroup;
        }

        // Filter these action group which have actions with locked target.
        IReadOnlyList<Models.ActionGroup.ActionGroup> filteredNextActionGroups = lockedNodeIds is { Count: > 0 } ? notStartedActionGroups.Where(g => g.Actions.All(a => lockedNodeIds.All(ln => ln != a.Target.Node?.Id))).ToList() : notStartedActionGroups;

        List<Models.ActionGroup.ActionGroup> priorityActionGroups = filteredNextActionGroups.Where(a => a.HasPriority).ToList();
        Models.ActionGroup.ActionGroup? nextActionGroup;
        if (priorityActionGroups.Count > 0)
        {
            nextActionGroup = await SelectNextActionGroup(agv, agvData, cancellationToken, priorityActionGroups, []);
            if (nextActionGroup != null)
            {
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

        List<Models.ActionGroup.ActionGroup> nextActionGroups = filteredNextActionGroups.Except(priorityActionGroups).ToList();

        List<Models.ActionGroup.ActionGroup> nextActionGroupsForAgv = nextActionGroups.Where(a => a.AssignedAgv == agv.CorrelationId).ToList();
        if (agv.IsQueueing)
        {
            _logger.LogWarning("Prevent selecting next pick drop action group for queueing agv {AgvCorrelationId}", agv.CorrelationId);
            return null;
        }

        if (nextActionGroupsForAgv.Count > 0)
        {
            nextActionGroup = await SelectNextActionGroup(agv, agvData, cancellationToken, nextActionGroupsForAgv, []);
            if (nextActionGroup != null)
            {
                return nextActionGroup;
            }
        }
        nextActionGroup = await SelectNextActionGroup(agv, agvData, cancellationToken, nextActionGroups.Except(nextActionGroupsForAgv).ToList(), nodesToAvoid);
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