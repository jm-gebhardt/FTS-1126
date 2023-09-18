using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides handlers for <see cref="Action"/> related commands.
/// </summary>
[UsedImplicitly]
public class ActionCommandHandler :
    IConsumer<StartAction>,
    IConsumer<TryStartManualAction>,
    IConsumer<TryStartNextActionInActionGroup>,
    IConsumer<TryStartNewActionGroup>,
    IConsumer<CreateParkingActionGroup>,
    IConsumer<CreateChargingActionGroup>,
    IConsumer<CreateMaintenanceActionGroup>,
    IConsumer<CompleteAction>,
    IConsumer<CancelActionGroup>
{
    private readonly ILogger<ActionCommandHandler> _logger;
    private readonly IRouteService _routeService;
    private readonly IAgvSagaCommunicationService _agvSagaCommunicationService;
    private readonly IActionGroupRegistry _actionGroupRegistry;
    private readonly IChooseAgvActionGroupService _chooseAgvActionGroupService;
    private readonly IActionManagerSemaphoreProvider _semaphoreProvider;
    private readonly IMaintenanceService _maintenanceService;
    private readonly IChargingPointManager _chargingPointManager;
    private readonly IParkPointService _parkPointService;
    private readonly IChargingService _chargingService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionCommandHandler"/> instance.
    /// </summary>
    public ActionCommandHandler(IMaintenanceService maintenanceService,
        IChargingPointManager chargingPointManager,
        IParkPointService parkPointService,
        IRouteService routeService,
        IAgvSagaCommunicationService agvSagaCommunicationService,
        IActionGroupRegistry actionGroupRegistry,
        IChooseAgvActionGroupService chooseAgvActionGroupService,
        IActionManagerSemaphoreProvider semaphoreProvider,
        IChargingService chargingService,
        IMapper mapper,
        ILogger<ActionCommandHandler> logger)
    {
        _logger = logger;
        _routeService = routeService;
        _agvSagaCommunicationService = agvSagaCommunicationService;
        _actionGroupRegistry = actionGroupRegistry;
        _chooseAgvActionGroupService = chooseAgvActionGroupService;
        _semaphoreProvider = semaphoreProvider;
        _parkPointService = parkPointService;
        _chargingPointManager = chargingPointManager;
        _maintenanceService = maintenanceService;
        _chargingService = chargingService;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the <see cref="StartAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<StartAction> context)
    {
        var (action, assignedAgvId) = context.Message;

        var cancellationToken = context.CancellationToken;

        var assignedAgv = await _agvSagaCommunicationService.GetAgvAsync(assignedAgvId, cancellationToken);

        var actionGroup = await _actionGroupRegistry.GetActionGroupAsync(action.ParentId);

        var dbAction = actionGroup?.Actions.FirstOrDefault(a => a.State != ActionState.Completed);

        if (dbAction == null)
        {
            _logger.LogError("Action could not be found in db for {ActionGroupId}, {CorrelationId}", action.ParentId, assignedAgvId);
            return;
        }
        _logger.LogDebug("Trying to start action {ActionId} with type {ActionType} for agv {CorrelationId}", dbAction.CorrelationId, Enum.GetName(dbAction.ActionType), assignedAgvId);

        await StartActionForAgv(context, dbAction, assignedAgv, cancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="TryStartManualAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryStartManualAction> context)
    {
        var agvCorrelationId = context.Message.Agv.CorrelationId;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvSagaCommunicationService.GetAgvAsync(agvCorrelationId, cancellationToken);
        if (!agv.IsLocked)
        {
            return;
        }

        IReadOnlyList<ActionGroup> actionGroups = await _actionGroupRegistry
            .GetActionGroupsAsync(ActionGroupState.Created, assignedAgvCorrelationId: agv.CorrelationId, isManual: true, maxItems: 1);

        var actionGroup = actionGroups.FirstOrDefault();
        if (actionGroup?.Actions.Count > 1)
        {
            throw new ArgumentException($"More than one actions in manual ActionGroup: {actionGroup}");
        }

        var action = actionGroup?.Actions.FirstOrDefault();

        if (action == null)
        {
            _logger.LogDebug("There is no next action available for Agv with CorrelationId {AgvCorrelationId}", agv.CorrelationId);
            await context.Publish(new AgvCanStartIdleAction(agv), cancellationToken);
            return;
        }

        await StartActionForAgv(context, action, agv, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="TryStartNextActionInActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryStartNextActionInActionGroup> context)
    {
        var agvCorrelationId = context.Message.Agv.CorrelationId;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvSagaCommunicationService.GetAgvAsync(agvCorrelationId, cancellationToken);
        if (!await _actionGroupRegistry.HasUnfinishedDropActionsAsync(agv.CorrelationId) || agv.IsLocked)
        {
            return;
        }
        _logger.LogInformation("Starting succeeding action for Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);
        try
        {
            var action = await _actionGroupRegistry.GetSubsequentActionAsync(agv.CorrelationId);
            await StartActionForAgv(context, action, agv, cancellationToken);
        }
        catch
        {
            // release Agv in case of error
            await _agvSagaCommunicationService.SetIdleAsync(agv.CorrelationId, cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Handles the <see cref="TryStartNewActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryStartNewActionGroup> context)
    {
        var agvCorrelationId = context.Message.Agv.CorrelationId;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvSagaCommunicationService.GetAgvAsync(agvCorrelationId, cancellationToken);

        bool needsCharging = _chargingService.AgvNeedsCharging(agv);
        bool isCarryingLoad = await _actionGroupRegistry.HasUnfinishedDropActionsAsync(agv.CorrelationId);
        bool needsMaintenance = _maintenanceService.MaintenanceRequested(agv);

        bool canStartNewAction = !needsCharging && !needsMaintenance && !isCarryingLoad && !agv.IsLocked;

        if (!canStartNewAction)
        {
            return;
        }

        await _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var actionGroup = await _chooseAgvActionGroupService.GetNotStartedActionGroupAsync(agv, cancellationToken);
            if (actionGroup == null)
            {
                _logger.LogDebug("There is no next action available for Agv with CorrelationId {AgvCorrelationId}", agv.CorrelationId);
                return;
            }
            var action = actionGroup.Actions.OrderBy(a => a.Sequence).First();
            _logger.LogDebug("Trying to start action {ActionId} with type {ActionType}", action.CorrelationId, Enum.GetName(action.ActionType));
            await StartActionForAgv(context, action, agv, context.CancellationToken);
        }, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="CreateParkingActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateParkingActionGroup> context)
    {
        var agvCorrelationId = context.Message.Agv.CorrelationId;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvSagaCommunicationService.GetAgvAsync(agvCorrelationId, cancellationToken);

        if (_chargingService.AgvIsEligibleForCharging(agv) || agv.IsLocked)
        {
            return;
        }

        if (await _parkPointService.IsOnAParkNodeAsync(agv) || await _actionGroupRegistry.IsAgvHeadingToNodeForActionOfType(agv.CorrelationId, AutomaticActionOrderId.AutomaticPark))
        {
            _logger.LogDebug("No need for a park action because the Agv is already in park for agvId: {AgvCorrelationId}", agv.CorrelationId);
            return;
        }
        _logger.LogInformation("Starting park action for Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);

        var parkNode = await _parkPointService.ReserveParkAsync(agv);

        var groupCorrelationId = Guid.NewGuid();
        var actionGroup = new ActionGroup(
            groupCorrelationId,
            nameof(AutomaticActionOrderId.AutomaticPark),
            actions: new List<AgvAction> {new(Guid.NewGuid(), ActionType.Move, parkNode.Id, groupCorrelationId)},
            assignedAgv: agv.CorrelationId,
            creator: ActionGroupCreator.TransportOrder
        );

        await _actionGroupRegistry.AddAsync(actionGroup);
        await context.Publish(new ActionGroupCreated(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), actionGroup.CorrelationId), cancellationToken);
        await context.Publish(new ActionGroupCreatedForAgv(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), agv.CorrelationId), cancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="CreateChargingActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateChargingActionGroup> context)
    {
        var agvCorrelationId = context.Message.Agv.CorrelationId;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvSagaCommunicationService.GetAgvAsync(agvCorrelationId, cancellationToken);

        bool needsCharging = _chargingService.AgvNeedsCharging(agv);
        bool isCarryingLoad = await _actionGroupRegistry.HasUnfinishedDropActionsAsync(agv.CorrelationId);

        bool shouldCharge = !isCarryingLoad && needsCharging;

        _logger.LogDebug("Evaluating {name} with {needsCharging}, {isCarryingLoad} for agv {correlationId}", nameof(CreateChargingActionGroup), needsCharging, isCarryingLoad, agv.CorrelationId);

        if (!_chargingService.AgvIsEligibleForCharging(agv) || !shouldCharge || agv.IsLocked)
        {
            return;
        }

        if (agv.GetRequiredAgvStatus().Charging || await _actionGroupRegistry.IsAgvHeadingToNodeForActionOfType(agv.CorrelationId, AutomaticActionOrderId.AutomaticCharge))
        {
            _logger.LogDebug("No need for a another charge action because the Agv is already charging for agvId: {AgvId}", agv.AgvId);
            // This might happen if the AssignAsync action is called several times due to a low SOC value, although the Agv is already charging
            return;
        }
        _logger.LogInformation("Starting charge action for Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);

        var chargingNode = await _chargingPointManager.ReserveChargerAsync(agv);

        var groupCorrelationId = Guid.NewGuid();
        var actionGroup = new ActionGroup(
            groupCorrelationId,
            nameof(AutomaticActionOrderId.AutomaticCharge),
            actions: new List<AgvAction> {new(Guid.NewGuid(), ActionType.Charge, chargingNode.Id, groupCorrelationId)},
            assignedAgv: agv.CorrelationId,
            creator: ActionGroupCreator.TransportOrder
        );

        await _actionGroupRegistry.AddAsync(actionGroup);
        await context.Publish(new ActionGroupCreated(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), actionGroup.CorrelationId), cancellationToken);
        await context.Publish(new ActionGroupCreatedForAgv(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), agv.CorrelationId), cancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="CreateMaintenanceActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateMaintenanceActionGroup> context)
    {
        var agvCorrelationId = context.Message.Agv.CorrelationId;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvSagaCommunicationService.GetAgvAsync(agvCorrelationId, cancellationToken);

        bool needsCharging = _chargingService.AgvNeedsCharging(agv);
        bool isCarryingLoad = await _actionGroupRegistry.HasUnfinishedDropActionsAsync(agv.CorrelationId);
        bool needsMaintenance = _maintenanceService.MaintenanceRequested(agv);

        bool shouldCharge = !isCarryingLoad && needsCharging;
        bool shouldDoMaintenance = !isCarryingLoad && needsMaintenance;
        if (shouldCharge || !shouldDoMaintenance || agv.IsLocked)
        {
            return;
        }

        if (await _actionGroupRegistry.IsAgvHeadingToNodeForActionOfType(agv.CorrelationId, AutomaticActionOrderId.AutomaticMaintenance))
        {
            return;
        }

        _logger.LogInformation("Starting maintenance action for Agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);

        var maintenanceNode = await _maintenanceService.GetMaintenanceNode(agv);

        var groupCorrelationId = Guid.NewGuid();
        var actionGroup = new ActionGroup(
            groupCorrelationId,
            nameof(AutomaticActionOrderId.AutomaticMaintenance),
            actions: new List<AgvAction> {new(Guid.NewGuid(), ActionType.Move, maintenanceNode.Id, groupCorrelationId)},
            assignedAgv: agv.CorrelationId,
            creator: ActionGroupCreator.TransportOrder
        );

        await _actionGroupRegistry.AddAsync(actionGroup);
        await context.Publish(new ActionGroupCreatedForAgv(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), agv.CorrelationId), cancellationToken);
        await context.Publish(new ActionGroupCreated(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), actionGroup.CorrelationId), cancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="CompleteAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CompleteAction> context)
    {
        var correlationId = context.Message.ActionCorrelationId;
        var cancellationToken = context.CancellationToken;

        _logger.LogDebug("Completing action with CorrelationId: {ActionCorrelationId}", correlationId);
        var action = await _actionGroupRegistry.GetRequiredActionByIdAsync(correlationId);

        var actionGroup = await _actionGroupRegistry.GetRequiredActionGroupByIdAsync(action.ParentId);
        var agv = await _agvSagaCommunicationService.GetAgvAsync(actionGroup.AssignedAgv, cancellationToken);

        actionGroup = await _actionGroupRegistry.UpdateActionAndGroupStateAsync(action, agv, ActionState.Completed);
        var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);
        if (actionGroup.State == ActionGroupState.Completed)
        {
            await context.Publish(new ActionGroupCompleted(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);
        }

        // charge will switch to charging state and can not be released immediately
        if (action.ActionType != ActionType.Charge)
        {
            _logger.LogDebug("Setting Agv with CorrelationId: {AgvCorrelationId} to idle, because action has been completed and type is not charge", agv.CorrelationId);

            await _agvSagaCommunicationService.SetIdleAsync(agv.CorrelationId, cancellationToken);
        }
    }

    /// <summary>
    /// Handles the <see cref="CancelActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CancelActionGroup> context)
    {
        var actionGroupId = context.Message.CorrelationId;
        var actionGroup = await _actionGroupRegistry.GetActionGroupAsync(actionGroupId);

        if (actionGroup == null)
        {
            await context.Publish(new ActionGroupCancelFailed(null!, actionGroupId, HostOrderCancelFailedErrorCode.OrderDoesNotExist));
            return;
        }
        var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);

        switch (actionGroup.State)
        {
            case ActionGroupState.Started:
                await context.Publish(new ActionGroupCancelFailed(actionGroupMessageContent, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderAlreadyStarted));
                return;

            case ActionGroupState.Completed:
                await context.Publish(new ActionGroupCancelFailed(actionGroupMessageContent, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderAlreadyFinished));
                return;

            case ActionGroupState.Failed:
                await context.Publish(new ActionGroupCancelFailed(actionGroupMessageContent, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderAlreadyFailed));
                return;

            case ActionGroupState.Canceled:
                await context.Publish(new ActionGroupCancelFailed(actionGroupMessageContent, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderAlreadyCancelled));
                return;
        }
        var agv = await _agvSagaCommunicationService.GetAgvAsync(actionGroup.AssignedAgv, context.CancellationToken);

        actionGroup = await _actionGroupRegistry.UpdateActionAndGroupStateAsync(actionGroup.Actions.First(), agv, ActionState.Canceled);
        await context.Publish(new ActionGroupCancelled(actionGroupMessageContent, actionGroup.CorrelationId));
    }

    private async Task StartActionForAgv(IPublishEndpoint context, AgvAction action, IAgv assignedAgv, CancellationToken cancellationToken)
    {
        action.Nodes = (await _routeService.GetRouteAsync(assignedAgv, action.TargetNodeId, cancellationToken)).Nodes;

        if (!await _agvSagaCommunicationService.IsAgvReadyForBusyAsync(assignedAgv.CorrelationId, cancellationToken, ignoreCharging: true, ignoreLocked: true))
        {
            _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} is not ready to be set to busy", assignedAgv.CorrelationId);
            return;
        }

        _logger.LogDebug("Starting action with CorrelationId: {ActionCorrelationId} for Agv {AgvCorrelationId}. Setting Agv to busy in next step.", action.CorrelationId, assignedAgv.CorrelationId);
        await _agvSagaCommunicationService.SetBusyAsync(assignedAgv, true, true, cancellationToken);
        try
        {
            if (action.Nodes == null || !action.Nodes.Any())
            {
                throw new ArgumentNullException(nameof(action.Nodes));
            }

            await context.Publish(new StartActionCommand(assignedAgv.CorrelationId, assignedAgv.AgvId, _mapper.Map<Common.Messages.ActionGroups.AgvAction>(action)), cancellationToken);
            _logger.LogDebug("Action with CorrelationId: {ActionCorrelationId} for Agv {AgvCorrelationId} has been started. Updating ActionGroup.State to Started in next step.", action.CorrelationId, assignedAgv.CorrelationId);
            var actionGroup = await _actionGroupRegistry.UpdateActionAndGroupStateAsync(action, assignedAgv, ActionState.Started);
            var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
            await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);

            bool isFirstActionOrderByHost = action is {Sequence: 0, ActionType: ActionType.Pick};
            if (isFirstActionOrderByHost)
            {
                await context.Publish(new ActionGroupStarted(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);
            }
            _logger.LogTrace("Starting action done");
        }
        catch
        {
            // release Agv in case of error
            await _agvSagaCommunicationService.SetIdleAsync(assignedAgv.CorrelationId, cancellationToken);
            throw;
        }
    }
}