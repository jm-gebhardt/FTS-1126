using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides action related methods.
/// </summary>
[UsedImplicitly]
public class ActionCommandHelper
{
    private readonly ILogger<ActionCommandHelper> _logger;
    private readonly IRouteService _routeService;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IChooseAgvActionGroupService _chooseAgvActionGroupService;
    private readonly ISemaphoreProvider<IAgvsSemaphore> _actionManagerSemaphoreProvider;
    private readonly IMapper _mapper;
    private readonly IBus _bus;
    private readonly IAgvDb _agvDb;

    /// <inheritdoc cref="ActionCommandHelper"/>>
    public ActionCommandHelper(ILogger<ActionCommandHelper> logger, IRouteService routeService, IAgvDb agvDb, IActionGroupRepository actionGroupRepository, IChooseAgvActionGroupService chooseAgvActionGroupService, ISemaphoreProvider<IAgvsSemaphore> actionManagerSemaphoreProvider, IBus bus, IMapper mapper)
    {
        _logger = logger;
        _routeService = routeService;
        _agvDb = agvDb;
        _actionGroupRepository = actionGroupRepository;
        _chooseAgvActionGroupService = chooseAgvActionGroupService;
        _actionManagerSemaphoreProvider = actionManagerSemaphoreProvider;
        _mapper = mapper;
        _bus = bus;
    }

    /// <summary>
    /// Select nodes for an action.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="assignedAgv">The assigned agv.</param>
    /// <param name="ignoreLocked">Ignore of the assigned agv is locked.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<bool> SelectNodesForAgvAction(AgvAction action, Domain.Models.Agv.Agv assignedAgv, bool ignoreLocked, CancellationToken cancellationToken)
    {
        if (!assignedAgv.TryTransitionToBusy(ignoreLocked: ignoreLocked))
        {
            _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} is not ready to be set to busy.", assignedAgv.CorrelationId);
            return false;
        }

        // here the state has changed in all cases
        await PublishStateChange(assignedAgv);

        var agvData = await _agvDb.Data.GetAgvDataAsync(assignedAgv.CorrelationId);
        var route = await _routeService.GetRouteAsync(assignedAgv, action.TargetNodeId, null, agvData, cancellationToken);
        action.Nodes = action.ActionType == ActionType.Charge ? [action.TargetNodeId] : route.Nodes;
        action.Edges = route.Edges;

        _logger.LogDebug("Starting action with CorrelationId: {ActionCorrelationId} of group {ActionGroupCorrelationId} for Agv {AgvCorrelationId}. Setting Agv to busy in next step.", action.CorrelationId, action.ParentId, assignedAgv.CorrelationId);
        try
        {
            if ((action.Nodes == null || !action.Nodes.Any()) && assignedAgv.AgvStatus!.LastNodeId != action.TargetNodeId)
            {
                throw new ArgumentNullException(nameof(action.Nodes));
            }
        }
        catch
        {
            _logger.LogWarning("Node selection failed for action: {Action} of group {ActionGroupCorrelationId}.", action, action.ParentId);
            return false;
        }
        return true;
    }

    private async Task PublishStateChange(Domain.Models.Agv.Agv agv)
    {
        var agvMessageContent = _mapper.Map<Agv>(agv);
        await _bus.Publish(new AgvStateChanged(agv.CorrelationId, agv.CurrentAgvState, agv.PreviousAgvState, agvMessageContent));
    }

    /// <summary>
    /// Try update action group.
    /// </summary>
    /// <param name="actionGroup">The action group.</param>
    public async Task TryUpdateActionGroup(Domain.Models.ActionGroup.ActionGroup actionGroup)
    {
        try
        {
            await _actionGroupRepository.UpdateAsync(actionGroup);
        }
        catch (SqlException ex)
        {
            _logger.LogWarning("Update of action group caught {Exception}. Retrying ...", ex.Message);
            await Task.Delay(1000);
            await TryUpdateActionGroup(actionGroup);
        }
    }

    private async Task<Domain.Models.Agv.Agv?> SelectAgvForActionGroup(Domain.Models.ActionGroup.ActionGroup actionGroup, CancellationToken cancellationToken)
    {
        Domain.Models.Agv.Agv? agv = null;
        await _actionManagerSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            ICollection<Domain.Models.Agv.Agv> goodAgvs = (await _agvDb.Agvs.GetReadyAgvsWithAtLeastLowBatteryLevel()).ToList();
            ICollection<AgvData> agvData = await _agvDb.Data.GetSelectedAgvDataAsync(goodAgvs.Select(a => a.CorrelationId).ToList());
            agv = await _chooseAgvActionGroupService.GetAgvAsync(actionGroup, goodAgvs.ToList(), agvData.ToList(), cancellationToken);
        }, cancellationToken);

        if (agv != null && !actionGroup.FirstNotStartedAction!.IsIncomplete)
        {
            actionGroup.Assign(agv);
        }
        return agv;
    }

    /// <summary>
    /// Get an agv for an action group (either from db or select a new one).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="assignedAgv">The assigned agv.</param>
    /// <param name="actionGroup">The action group.</param>
    /// <returns>An agv or null if none was selected.</returns>
    public async Task<Domain.Models.Agv.Agv?> GetAgvForActionGroup(CancellationToken cancellationToken, Guid? assignedAgv, Domain.Models.ActionGroup.ActionGroup actionGroup)
    {
        if (assignedAgv == null && actionGroup.AssignedAgv == null)
        {
            var agv = await SelectAgvForActionGroup(actionGroup, cancellationToken);
            if (agv != null)
            {
                actionGroup.Assign(agv);
            }
            return agv;
        }

        if(assignedAgv == null)
        {
            return  await _agvDb.Agvs.GetAgvAsync(actionGroup.AssignedAgv!.Value);
        }

        return  await _agvDb.Agvs.GetAgvAsync(assignedAgv.Value);
    }

    /// <summary>
    /// Check if an action can be started.
    /// </summary>
    /// <param name="context">The context for publishing.</param>
    /// <param name="action">The action to be started.</param>
    /// <param name="assignedAgv">The assigned agv.</param>
    /// <param name="actionGroup">The action group.</param>
    /// <returns></returns>
    public async Task<bool> CheckAgvActionCanBeStarted(IPublishEndpoint context, AgvAction action, Domain.Models.Agv.Agv assignedAgv, Domain.Models.ActionGroup.ActionGroup actionGroup)
    {
        if (actionGroup.State == ActionGroupState.Paused)
        {
            _logger.LogWarning("Preventing start of action {Action} of group {ActionGroupCorrelationId}, because the action group is paused.", action.CorrelationId, actionGroup.CorrelationId);
            await context.Publish(new StartingActionForAgvFailed(assignedAgv.CorrelationId));
            return false;
        }

        if (assignedAgv.IsLocked)
        {
            _logger.LogWarning("Preventing start of action {Action} of group {ActionGroupCorrelationId}, because agv {CorrelationId} can not start any actions in mode {Mode}.", action.CorrelationId, actionGroup.CorrelationId, assignedAgv.CorrelationId, assignedAgv.Mode.Type.ToString());
            await context.Publish(new StartingActionForAgvFailed(assignedAgv.CorrelationId));
            return false;
        }

        if (actionGroup is { IsMovingCargo: true, IsManual: true } && !assignedAgv.CanStartManualPickDropAction)
        {
            _logger.LogWarning("Preventing start of action {Action} of group {ActionGroupCorrelationId}, because agv {CorrelationId} can not start pick/drop actions in mode {Mode}.", action.CorrelationId, actionGroup.CorrelationId, assignedAgv.CorrelationId, assignedAgv.Mode.Type.ToString());
            await context.Publish(new PauseActionGroup(actionGroup.CorrelationId));
            await context.Publish(new StartingActionForAgvFailed(assignedAgv.CorrelationId));
            return false;
        }

        if (!actionGroup.IsManual && !assignedAgv.CanStartAutomaticAction)
        {
            _logger.LogWarning("Preventing start of action {Action} of group {ActionGroupCorrelationId}, because agv {CorrelationId} can not start automatic action in mode {Mode}.", action.CorrelationId, actionGroup.CorrelationId, assignedAgv.CorrelationId, assignedAgv.Mode.Type.ToString());
            await context.Publish(new PauseActionGroup(actionGroup.CorrelationId));
            await context.Publish(new StartingActionForAgvFailed(assignedAgv.CorrelationId));
            return false;
        }
        
        bool isNextActionInGroup = actionGroup.FirstNotStartedAction!.CorrelationId == action.CorrelationId;
        if (!isNextActionInGroup)
        {
            _logger.LogWarning("Preventing start of action {Action} of group {ActionGroupCorrelationId}, because the action of another group should start first.", action.CorrelationId, actionGroup.CorrelationId);
            return false;
        }

        if (actionGroup.AssignedAgv.GetValueOrDefault() != assignedAgv.CorrelationId)
        {
            _logger.LogError("Could not start action {Action} of group {ActionGroupCorrelationId} for agv {AgvCorrelationId}, the action group is already handled by {OtherAgvCorrelationId}.", action.CorrelationId, action.ParentId, assignedAgv.CorrelationId, actionGroup.AssignedAgv);
            await context.Publish(new StartingActionForAgvFailed(assignedAgv.CorrelationId));
            return false;
        }

        if (action.IsIncomplete)
        {
            _logger.LogError("Could not action {Action} of group {ActionGroupCorrelationId} for agv {AgvCorrelationId}, the action group is incomplete.", action.CorrelationId, action.ParentId, assignedAgv.CorrelationId);
            await context.Publish(new StartingActionForAgvFailed(assignedAgv.CorrelationId));
            return false;
        }

        IReadOnlyList<Domain.Models.ActionGroup.ActionGroup> startedActionGroups = await _actionGroupRepository.GetActionGroupsAsync(new List<ActionGroupState> { ActionGroupState.Started }, assignedAgvCorrelationId: assignedAgv.CorrelationId);
        bool otherActionGroupsStarted = startedActionGroups.Any(a => a.CorrelationId != action.ParentId);
        if (!otherActionGroupsStarted)
        {
            return true;
        }

        _logger.LogError("Could not start action {Action} of group {ActionGroupCorrelationId}, there are other started action groups for this agv.", action.CorrelationId, action.ParentId);
        await context.Publish(new StartingActionForAgvFailed(assignedAgv.CorrelationId));
        return false;
    }
}