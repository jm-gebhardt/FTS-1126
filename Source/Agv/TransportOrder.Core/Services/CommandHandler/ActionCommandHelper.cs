using AutoMapper;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Agv = Gebhardt.Acc.TransportOrder.Messages.MessageContent.Agv;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides action related methods.
/// </summary>
[UsedImplicitly]
public class ActionCommandHelper
{
    private readonly ILogger<ActionCommandHelper> _logger;
    private readonly IStationRepository _stationRepository;
    private readonly IRouteService _routeService;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IMapper _mapper;
    private readonly IBus _bus;
    private readonly IAgvDb _agvDb;

    /// <inheritdoc cref="ActionCommandHelper"/>>
    public ActionCommandHelper(ILogger<ActionCommandHelper> logger,
        IRouteService routeService,
        IAgvDb agvDb,
        IActionGroupRepository actionGroupRepository,
        IStationRepository stationRepository,
        IBus bus,
        IMapper mapper)
    {
        _logger = logger;
        _routeService = routeService;
        _agvDb = agvDb;
        _actionGroupRepository = actionGroupRepository;
        _stationRepository = stationRepository;
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
        bool lastActionWasMoveToCoordinate = await _actionGroupRepository.IsLastCompletedActionMoveToCoordinate(assignedAgv.CorrelationId);
        if (!lastActionWasMoveToCoordinate && !action.IsMoveToCoordinate)
        {
            var agvData = await _agvDb.Data.GetAgvDataAsync(assignedAgv.CorrelationId);
            var route = await _routeService.GetRouteAsync(assignedAgv, action.Target.NodeId!, null, agvData, cancellationToken);
            action.Nodes = action.ActionType == ActionType.Charge ? [action.Target.NodeId!] : route.Nodes;
            action.Edges = route.Edges;
        }

        if (lastActionWasMoveToCoordinate)
        {
            action.Nodes = [action.Target.NodeId!];
            action.Edges = [];
        }

        if (action.IsMoveToCoordinate)
        {
            action.Edges = [];
            action.Nodes = [];
        }

        try
        {
            if ((action.Nodes == null || !action.Nodes.Any()) && assignedAgv.AgvStatus!.LastNodeId != action.Target.NodeId! && !action.IsMoveToCoordinate)
            {
                throw new ArgumentNullException(nameof(action.Nodes));
            }
        }
        catch
        {
            _logger.LogWarning("Node selection failed for action: {Action} of group {ActionGroupCorrelationId}.", action, action.ParentId);
            return false;
        }

        _logger.LogDebug(
            "Starting action with CorrelationId: {ActionCorrelationId} of group {ActionGroupCorrelationId} for Agv {AgvCorrelationId}. AGV set to busy",
            action.CorrelationId,
            action.ParentId,
            assignedAgv.CorrelationId);
        return true;
    }

    /// <summary>
    /// Log update in station context
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <param name="action">The action.</param>
    /// <param name="actionGroup">The action group.</param>
    /// <returns></returns>
    public async Task LogStationUpdate(Domain.Models.Agv.Agv agv, AgvAction action, Domain.Models.ActionGroup.ActionGroup actionGroup)
    {
        if (action.Target.NodeId == null)
        {
            return;
        }

        try
        {
            var station = await _stationRepository.GetStation(action.Target.NodeId);
            switch (action.ActionType)
            {
                case ActionType.Drop:
                    _logger.LogStationInformation(LogCodes.StationAgvDropping, station.Id, "AGV Dropping",
                        $"AGV {agv.SerialNumber} is heading towards station to drop order {actionGroup.OrderId}",
                        new Dictionary<string, object?>
                        {
                            { LogLabels.AgvSerialNumber, agv.SerialNumber }, { LogLabels.OrderId, actionGroup.OrderId }
                        });
                    break;

                case ActionType.Pick:
                    _logger.LogStationInformation(LogCodes.StationAgvPicking, station.Id, "Agv Picking",
                        $"AGV {agv.SerialNumber} is heading towards station to pick order {actionGroup.OrderId}",
                        new Dictionary<string, object?>
                        {
                            { LogLabels.AgvSerialNumber, agv.SerialNumber }, { LogLabels.OrderId, actionGroup.OrderId }
                        });
                    break;

                case ActionType.Move:
                    _logger.LogStationInformation(LogCodes.StationAgvMoving, station.Id, "AGV Moving",
                        $"AGV {agv.SerialNumber} is heading towards station", new Dictionary<string, object?>
                        {
                            { LogLabels.AgvSerialNumber, agv.SerialNumber }
                        });
                    break;

                case ActionType.Charge:
                    _logger.LogStationInformation(LogCodes.StationAgvCharging, station.Id, "AGV Charging",
                        $"AGV {agv.SerialNumber} starts charging at station", new Dictionary<string, object?>
                        {
                            { LogLabels.AgvSerialNumber, agv.SerialNumber }
                        });
                    break;
            }
        }
        catch (Exception)
        {
            // ignored
        }
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
            var pauseReason = assignedAgv.Mode.GetPauseReason();
 
            await context.Publish(new PauseActionGroup(actionGroup.CorrelationId, pauseReason));
            await context.Publish(new StartingActionForAgvFailed(assignedAgv.CorrelationId));
            return false;
        }

        if (!actionGroup.IsManual && !assignedAgv.CanStartAutomaticAction)
        {
            _logger.LogWarning("Preventing start of action {Action} of group {ActionGroupCorrelationId}, because agv {CorrelationId} can not start automatic action in mode {Mode}.", action.CorrelationId, actionGroup.CorrelationId, assignedAgv.CorrelationId, assignedAgv.Mode.Type.ToString());

            var pauseReason = assignedAgv.Mode.GetPauseReason();

            await context.Publish(new PauseActionGroup(actionGroup.CorrelationId, pauseReason));
            await context.Publish(new StartingActionForAgvFailed(assignedAgv.CorrelationId));
            return false;
        }
        if (actionGroup.FirstNotStartedAction == null)
        {
            _logger.LogWarning("Preventing start of action {ActionCorrelationId} of group {ActionGroupCorrelationId} there is no action that can be started, {Action}, {ActionInGroup}", action.CorrelationId, actionGroup.CorrelationId, action.ToString(), actionGroup.Actions.Single(a => a.CorrelationId  ==action.CorrelationId).ToString());
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

        // Does the AGV have a started action group? If not or the started action group is the action group we are attempting
        // to start, we can continue.
        var startedActionGroup = await _actionGroupRepository.GetStartedActionGroupForAgvAsync(assignedAgv.CorrelationId);
        if (startedActionGroup == null || startedActionGroup.CorrelationId == action.ParentId)
        {
            return true;
        }

        _logger.LogError("Could not start action {Action} of group {ActionGroupCorrelationId}, there are other started action groups for this agv.", action.CorrelationId, action.ParentId);
        await context.Publish(new StartingActionForAgvFailed(assignedAgv.CorrelationId));
        return false;
    }

    /// <summary>
    /// Get the next manual action for the given agv.
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <returns>The manual action.</returns>
    public async Task<AgvAction?> GetManualAction(Domain.Models.Agv.Agv agv)
    {
        List<ActionType>? allowedActionTypes = null;
        if (agv.CanStartManualPickDropAction)
        {
            allowedActionTypes = [ActionType.Pick, ActionType.Drop];
        }

        if (agv.CanStartManualOperationalAction)
        {
            if (allowedActionTypes == null)
            {
                allowedActionTypes = [ActionType.Charge, ActionType.Move];
            }
            else
            {
                allowedActionTypes.AddRange([ActionType.Charge, ActionType.Move]);
            }
        }

        var actionGroup = await _actionGroupRepository.GetNextManualActionGroupForAgvAsync(agv.CorrelationId, allowedActionTypes);
        if (actionGroup == null)
        {
            _logger.LogInformation("There is no manual action group available for agv with CorrelationId {AgvCorrelationId}.", agv.CorrelationId);
            return null;
        }
        return actionGroup.FirstNotStartedAction;
    }

    /// <summary>
    /// Checks if a station is locked if it exists.
    /// </summary>
    /// <param name="name">The name of the station.</param>
    public async Task<bool> StationIsLockedIfExists(string name)
    {
        try
        {
            return (await _stationRepository.GetStation(name)).IsLocked;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Handles skipped actions.
    /// </summary>
    /// <param name="context">Publish endpoint.</param>
    /// <param name="action">The action to be skipped.</param>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AgvAction?> HandleSkippedActions(IPublishEndpoint context, AgvAction action, Guid agvCorrelationId, CancellationToken cancellationToken)
    {
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByActionIdAsync(action.CorrelationId);

        var notSkippedAction = actionGroup.GetNextNotSkippedAction(action.CorrelationId);

        if (notSkippedAction == null)
        {
            await context.Publish(new AgvBecameIdle(agvCorrelationId), cancellationToken);
        }

        return notSkippedAction;
    }
}