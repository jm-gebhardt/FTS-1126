using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <summary>
/// Base class for ActionManager which provides implementations for creating methods (Pick, Drop, Charge and Maintenance) and handles CRUD operations for <see cref="AgvAction"/>
/// </summary>
public abstract class BaseActionManager
{
    private readonly IAgvActionRegistry _agvActionRegistry;
    private readonly IRouteService _routeService;
    private readonly ILogger<BaseActionManager> _logger;
    private readonly IChargingPointManager _chargingPointManager;
    private readonly IMaintenanceService _maintenanceService;
    private readonly IAgvRegistry _agvRegistry;
    private readonly IStartActionService _startActionService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="agvActionRegistry">Abstracted interface for Agv action registry</param>
    /// <param name="routeService">Service for computation of Route for Agv</param>
    /// <param name="chargingPointManager">Finds and Reserves charging node for agv</param>
    /// <param name="maintenanceService">Providing information about maintenance requests for Agvs</param>
    /// <param name="startActionService"></param>
    /// <param name="logger">generic interface for logging</param>
    /// <param name="agvRegistry">Interface for accessing the AGV registry</param>
    protected BaseActionManager(IAgvActionRegistry agvActionRegistry, IRouteService routeService, IChargingPointManager chargingPointManager, IMaintenanceService maintenanceService, IStartActionService startActionService, ILogger<BaseActionManager> logger, IAgvRegistry agvRegistry)
    {
        _agvActionRegistry = agvActionRegistry;
        _routeService = routeService;
        _chargingPointManager = chargingPointManager;
        _maintenanceService = maintenanceService;
        _logger = logger;
        _agvRegistry = agvRegistry;
        _startActionService = startActionService;
    }

    /// <summary>
    /// Main logic: Assigns a new action to the agv
    /// </summary>
    /// <param name="agv">The Agv to decide for</param>
    /// <param name="cancellationToken"></param>
    [UsedImplicitly]
    public abstract Task AssignAsync(IAgv agv, CancellationToken cancellationToken);

    /// <summary>
    /// Send <see cref="StartActionCommand"/> and set action to start <see cref="OrderActionState.Started"/>.
    /// </summary>
    protected async Task StartAction(AgvAction action, CancellationToken cancellationToken)
    {
        AddOrUpdateActionState(action, ActionState.Started);

        await _startActionService.StartAction(action, cancellationToken);
    }

    /// <summary>
    /// Get agv action by correlation Id for Action Event consumption
    /// </summary>
    /// <param name="correlationId"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <returns></returns>
    /// <exception cref="ActionNotFoundException"></exception>
    protected AgvAction GetAgvAction<TMessage>(Guid correlationId) where TMessage : class, CorrelatedBy<Guid>, IActionEventParent<string>
    {
        var action = _agvActionRegistry.GetActionById(correlationId);
        if (action != null)
        {
            return action;
        }

        _logger.LogError("{MessageType} but no corresponding action found for ActionCorrelationId: {ActionCorrelationId}", nameof(TMessage), correlationId);
        throw new ActionNotFoundException(nameof(TMessage), nameof(correlationId), correlationId);
    }

    /// <summary>
    /// Gets agv by correlation id
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    /// <exception cref="AgvNotFoundException"></exception>
    protected IAgv GetAgvByCorrelationId(Guid? correlationId)
    {
        var agv = _agvRegistry.GetAgv(correlationId);
        if (agv == null)
        {
            throw new AgvNotFoundException(correlationId);
        }
        return agv;
    }

    private void AddOrUpdateActionState(AgvAction action, ActionState actionState)
    {
        var existingAction = _agvActionRegistry.GetActionById(action.CorrelationId);

        if (existingAction == null)
        {
            action.State = actionState;
            AddAgvAction(action);
        }
        else
        {
            UpdateActionState(action, actionState);
        }
    }

    /// <summary>
    /// Updates state of Action
    /// </summary>
    /// <param name="action">The action to update</param>
    /// <param name="actionState">The target state</param>
    /// <returns></returns>
    protected void UpdateActionState(AgvAction action, ActionState actionState) => _agvActionRegistry.UpdateState(action.CorrelationId, actionState);

    /// <summary>
    /// Saves Agv Action to registry
    /// </summary>
    /// <param name="agvAction"></param>
    protected void AddAgvAction(AgvAction agvAction) => _agvActionRegistry.Insert(agvAction);

    /// <summary>
    /// Get not started action groups, returns the first action in the first action group that Agv can handle all actions
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    /// <exception cref="AgvLocationUnknownException"></exception>
    public AgvAction? GetNextParentedByAction(IAgv agv)
    {
        if (agv.AgvStatus?.LastNodeId == null)
        {
            throw new AgvLocationUnknownException(agv);
        }

        IEnumerable<ActionGroup> notStartedActionGroups = _agvActionRegistry.GetNotStartedActionGroups();
        var nextParentedByAction = (notStartedActionGroups
            .Select(a => a.ActionList.Select(action =>
            {
                bool hasRoute = _routeService.HasRoute(agv, action.TargetNodeId, out float? costs);
                return (hasRoute, action, costs);
            }))
            .Where(actionGroup => actionGroup.All(al => al.hasRoute))
            // out of the ActionGroup: take action with lowest sequence number = pick
            // take ActionGroup where this pick action has the lowest costs
            .MinBy(actionGroup => actionGroup.MinBy(p => p.action.Sequence).costs)?
            .FirstOrDefault())?.action;

        return nextParentedByAction;
    }

    /// <summary>
    /// Get the succeeding assigned action (in most cases this will be a drop action)
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    /// <exception cref="ActionNotFoundException"></exception>
    protected AgvAction GetSucceedingAction(IAgv agv)
    {
        var subsequentAction = _agvActionRegistry.GetSubsequentAction(agv.AgvId); //returns order by sequence. first()
        if (subsequentAction == null)
        {
            throw new ActionNotFoundException(nameof(subsequentAction), nameof(agv.AgvId), agv.AgvId);
        }
        return subsequentAction;
    }

    /// <summary>
    /// Gets the charging action for Agv
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    protected AgvAction GetChargingAction(IAgv agv)
    {
        if (agv.AgvGraph == null)
        {
            throw new AgvGraphNotDefinedException(agv.CorrelationId);
        }

        var chargingNode = _chargingPointManager.ReserveBestCharger(agv.AgvGraph);
        return new AgvAction(
            Guid.NewGuid(),
            agv.AgvId,
            ActionType.Charge,
            string.Empty,
            Guid.Empty,
            chargingNode.Id);
    }

    /// <summary>
    /// Gets maintenance action for Agv
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    protected AgvAction GetMaintenanceAction(IAgv agv)
    {
        if (agv.AgvGraph == null)
        {
            throw new AgvGraphNotDefinedException(agv.CorrelationId);
        }

        var maintenanceNode = _maintenanceService.GetMaintenanceNode(agv);
        return new AgvAction(
            Guid.NewGuid(),
            agv.AgvId,
            ActionType.Move,
            string.Empty,
            Guid.Empty,
            maintenanceNode.Id);
    }

    /// <summary>
    /// Gets park action for Agv
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    protected static AgvAction GetParkAction(IAgv agv)
    {
        return new AgvAction(
            Guid.NewGuid(),
            agv.AgvId,
            ActionType.Move,
            string.Empty,
            Guid.Empty,
            "parkTestId");
    }
}