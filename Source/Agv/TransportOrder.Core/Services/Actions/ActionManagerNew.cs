using MassTransit;
using Microsoft.Extensions.Logging;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <summary>
/// Assigns new actions based for multi LHD (loading handling devices)
/// </summary>
public class ActionManagerNew :
    BaseActionManager,
    IConsumer<ActionGroup>,
    IConsumer<AgvBecameReady>,
    IConsumer<ActionCompletedEvent>,
    IConsumer<ChargingAgvsCanHandleActionsAgainEvent>
{
    private readonly ILogger<ActionManagerNew> _logger;
    private readonly IChargingService _chargingService;
    private readonly IMaintenanceService _maintenanceService;
    private readonly SemaphoreSlim _lockSemaphoreSlim = new(1, 1);
    private readonly IAgvLoadCapabilityService _agvLoadCapabilityService;

    /// <inheritdoc />
    public ActionManagerNew(IAgvActionRegistry agvActionRegistry, IRouteService routeService, IChargingPointManager chargingPointManager, IMaintenanceService maintenanceService, ILogger<ActionManagerNew> logger, IChargingService chargingService, IAgvRegistry agvRegistry, IAgvLoadCapabilityService agvLoadCapabilityService, IStartActionService startActionService)
        : base(agvActionRegistry, routeService, chargingPointManager, maintenanceService, startActionService, logger, agvRegistry)
    {
        _logger = logger;
        _chargingService = chargingService;
        _maintenanceService = maintenanceService;
        _agvLoadCapabilityService = agvLoadCapabilityService;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AgvBecameReady> context)
    {
        var correlationId = context.Message.CorrelationId;
        _logger.LogInformation("{MessageType} for AGV {AgvCorrelationId}", nameof(AgvBecameReady), correlationId);
        var agv = GetAgvByCorrelationId(correlationId);

        await AssignAsync(agv, context.CancellationToken);
    }

    /// <summary>
    /// Handle <see cref="ActionCompletedEvent"/> from DeviceCommunication
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public Task Consume(ConsumeContext<ActionCompletedEvent> context)
    {
        var action = GetAgvAction<ActionCompletedEvent>(context.Message.CorrelationId);
        var agv = GetAgvByCorrelationId(action.AssignedAgv);

        _logger.LogDebug("{MessageType} from AGV {AgvId} for order {OrderId}",
            nameof(ActionCompletedEvent), action.AgvId, action.OrderId);

        UpdateActionState(action, ActionState.Completed);
        return AssignAsync(agv, context.CancellationToken);
    }

    /// <summary>
    /// Handle <see cref="ActionGroup"/> incoming from HostOrderStateMachine
    /// </summary>
    /// <param name="context"></param>
    public Task Consume(ConsumeContext<ActionGroup> context)
    {
        var message = context.Message;
        foreach (var action in message.ActionList)
        {
            _logger.LogInformation("ActionGroup {ParentId} with ActionId: [{ActionId}] has been consumed... [{Type} at target: {TargetNodeId}]",
                message.CorrelationId, action.CorrelationId, action.ActionType, action.TargetNodeId);

            AddAgvAction(action);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handle <see cref="ChargingAgvsCanHandleActionsAgainEvent"/> incoming from ChargingService
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<ChargingAgvsCanHandleActionsAgainEvent> context)
    {
        List<IAgv> goodAgvs = _chargingService.GetChargingAgvsWithGoodBatteryLevel();

        // preliminary logic: just go through all potential available Agvs and try to start a new action
        // later this should select only the best agv out of the goodAgvs list
        foreach (var agv in goodAgvs)
        {
            var pendingAction = GetPendingAction(agv);

            if (pendingAction != null)
            {
                await AssignAsync(agv, context.CancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public override async Task AssignAsync(IAgv agv, CancellationToken cancellationToken)
    {
        await _lockSemaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            // Work on pending actions
            // or if there is nothing pending go either charge if possible or park
            var nextAgvAction = GetPendingAction(agv) ?? GetIdleAction(agv);

            await StartAction(nextAgvAction, cancellationToken);
        }
        finally
        {
            _lockSemaphoreSlim.Release();
        }
    }

    private AgvAction? GetPendingAction(IAgv agv)
    {
        bool needsCharging = _chargingService.AgvNeedsCharging(agv);
        bool isCarryingLoad = _agvLoadCapabilityService.IsCarryingLoad(agv.AgvId);
        bool canGetNewLoad = _agvLoadCapabilityService.CanGetNewLoad(agv.AgvId);
        bool needsMaintenance = _maintenanceService.MaintenanceRequested(agv);

        // Always go directly to maintenance or charge, as this is prioritized (only if we are fully unloaded)
        if (!isCarryingLoad)
        {
            if (needsCharging)
            {
                return GetChargingAction(agv);
            }
            if (needsMaintenance)
            {
                return GetMaintenanceAction(agv);
            }
        }
        else if (needsCharging || needsMaintenance)
        {
            // we are loaded, and we want to get unloaded, so that we can do the charging or maintenance, therefore complete current action group
            return GetSucceedingAction(agv);
        }

        // just get next action (complete any current ActionGroup or start a new ActionGroup)
        return GetNextAction(agv, isCarryingLoad, canGetNewLoad);
    }

    private AgvAction GetIdleAction(IAgv agv)
    {
        // if we can enhance the SOC, let's go charging
        if (_chargingService.AgvIsEligibleForCharging(agv))
        {
            return GetChargingAction(agv);
        }

        // otherwise do not block the road
        return GetParkAction(agv);
    }

    /// <summary>
    /// Determine what action to start depending on the isCarryingLoad/canGetNewLoad
    /// </summary>
    /// <param name="agv">The Agv to decide for</param>
    /// <param name="isCarryingLoad"></param>
    /// <param name="canGetNewLoad"></param>
    /// <returns></returns>
    // ReSharper disable once UnusedParameter.Local for future use
    private AgvAction? GetNextAction(IAgv agv, bool isCarryingLoad, bool canGetNewLoad)
    {
        if (isCarryingLoad)
        {
            return GetSucceedingAction(agv);
        }

        return GetNextParentedByAction(agv);
    }
}