using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.ActionGroup;
using AgvAction = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.AgvAction;
using MessageActionGroup = Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.ActionGroup;
using MessageAgvAction = Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.AgvAction;

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
    IConsumer<QueueActionGroup>,
    IConsumer<CancelActionGroup>,
    IConsumer<SelectNodesForAction>
{
    private readonly ILogger<ActionCommandHandler> _logger;
    private readonly IRouteService _routeService;
    private readonly IAgvRepository _agvRepository;
    private readonly IActionGroupRegistry _actionGroupRegistry;
    private readonly IChooseAgvActionGroupService _chooseAgvActionGroupService;
    private readonly IActionManagerSemaphoreProvider _actionManagerSemaphoreProvider;
    private readonly IMaintenanceService _maintenanceService;
    private readonly IChargingPointManager _chargingPointManager;
    private readonly IParkPointService _parkPointService;
    private readonly ISettingsProvider<AgvSettings> _settingsProvider;
    private readonly IMapper _mapper;
    private readonly IActionGroupValidationService _actionGroupValidationService;
    private readonly IActionGroupRetrievalService _actionGroupRetrievalService;

    /// <summary>
    /// Initializes a new <see cref="ActionCommandHandler"/> instance.
    /// </summary>
    public ActionCommandHandler(IMaintenanceService maintenanceService, 
        IChargingPointManager chargingPointManager, 
        IParkPointService parkPointService,
        IRouteService routeService,
        IAgvRepository agvRepository,
        IActionGroupRegistry actionGroupRegistry,
        IActionGroupValidationService actionGroupValidationService,
        IChooseAgvActionGroupService chooseAgvActionGroupService,
        IActionGroupRetrievalService actionGroupRetrievalService,
        IActionManagerSemaphoreProvider actionManagerSemaphoreProvider,
        ISettingsProvider<AgvSettings> settingsProvider,
        IMapper mapper,
        ILogger<ActionCommandHandler> logger)
    {
        _logger = logger;
        _routeService = routeService;
        _agvRepository = agvRepository;
        _actionGroupRegistry = actionGroupRegistry;
        _chooseAgvActionGroupService = chooseAgvActionGroupService;
        _actionManagerSemaphoreProvider = actionManagerSemaphoreProvider;
        _parkPointService = parkPointService;
        _chargingPointManager = chargingPointManager;
        _maintenanceService = maintenanceService;
        _actionGroupValidationService = actionGroupValidationService;
        _actionGroupRetrievalService = actionGroupRetrievalService;
        _settingsProvider = settingsProvider;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the <see cref="StartAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<StartAction> context)
    {
        var (action, agvCorrelationId) = context.Message;
        var cancellationToken = context.CancellationToken;
        var assignedAgv = await _agvRepository.GetAgvAsync(agvCorrelationId);
        ActionGroup? actionGroup;

        try
        {
            actionGroup = await _actionGroupRegistry.UpdateActionAndGroupStateAsync(action,assignedAgv, ActionState.Started);
            if (actionGroup.AssignedAgv == null)
            {
                throw new AgvNotFoundException(null);
            }
            var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
            await context.Publish(new StartActionCommand(assignedAgv.CorrelationId, assignedAgv.AgvId, _mapper.Map<MessageAgvAction>(action)), cancellationToken);
            await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);

            _logger.LogDebug("Action with CorrelationId: {ActionCorrelationId} for Agv {AgvCorrelationId} has been started.", action.CorrelationId, assignedAgv.CorrelationId);
            bool isFirstActionOrderByHost = action is { Sequence: 0, ActionType: ActionType.Pick };
            if (isFirstActionOrderByHost)
            {
                await context.Publish(new ActionGroupStarted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow), cancellationToken);
            }
        }
        catch(AgvNotFoundException)
        {
           actionGroup = await _actionGroupRegistry.UpdateActionAndGroupStateAsync(action,action.Sequence == 0?assignedAgv: null, ActionState.Failed);
            _logger.LogError("Could not start action {Action} of group {ActionGroup}, no assigned agv.", action.CorrelationId, actionGroup.CorrelationId);
            await context.Publish(new StartingActionForAgvFailed(assignedAgv.CorrelationId), cancellationToken);
        }
    }

    /// <summary>
    /// Handles the <see cref="SelectNodesForAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SelectNodesForAction> context)
    {
        var (actionMessageContent, assignedAgvId) = context.Message;

        var cancellationToken = context.CancellationToken;

        var assignedAgv = await _agvRepository.GetAgvAsync(assignedAgvId);
    
        var actionGroup = await _actionGroupRegistry.GetActionGroupAsync(actionMessageContent.ParentId);

        var action = actionGroup?.FirstNotStartedAction;

        if (action == null)
        {
            _logger.LogError("Could not find un-started action for {ActionGroupId}, {CorrelationId}.", actionMessageContent.ParentId, assignedAgvId);
            return;
        }
        _logger.LogDebug("Trying to start action {ActionId} with type {ActionType} for agv {CorrelationId}.", action.CorrelationId, Enum.GetName(action.ActionType), assignedAgvId);

        if (actionMessageContent.CorrelationId != action.CorrelationId)
        {
            _logger.LogWarning("Action in message for which nodes are to be selected {ActionInMessage} is not the first not started action {NotStartedAction}.", actionMessageContent.CorrelationId, action.CorrelationId);
        }

        if (assignedAgv.CorrelationId != actionGroup!.AssignedAgv!)
        {
            _logger.LogWarning("Agv in message for which nodes are to be selected {AgvInMessage} is not the agv assigned to the group {AgvAssignedToGroup}.", assignedAgvId, actionGroup.AssignedAgv!);
        }

        await SelectNodesForAgvAction(context, action, assignedAgv, cancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="TryStartManualAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryStartManualAction> context)
    {
        var agvCorrelationId = context.Message.AgvCorrelationId;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvRepository.GetAgvAsync(agvCorrelationId);
 
        IReadOnlyList<ActionGroup> actionGroups = await _actionGroupRegistry
            .GetActionGroupsAsync(ActionGroupState.Created, assignedAgvCorrelationId: agv.CorrelationId, isManual: true, maxItems: 1);

       if (!actionGroups.Any())
        {
            _logger.LogDebug("There is no next action group available for agv with CorrelationId {AgvCorrelationId}.", agv.CorrelationId);
            await context.Publish(new AgvCanStartIdleAction(agv.CorrelationId), cancellationToken);
            return;
        }

        var action = actionGroups[0].FirstNotStartedAction;

        if (action == null)
        {
            _logger.LogDebug("There is no next action available for agv with CorrelationId {AgvCorrelationId}.", agv.CorrelationId);
            await context.Publish(new AgvCanStartIdleAction(agv.CorrelationId), cancellationToken);
            return;
        }

        await SelectNodesForAgvAction(context, action, agv, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="TryStartNextActionInActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryStartNextActionInActionGroup> context)
    {
        var agvCorrelationId = context.Message.AgvCorrelationId;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvRepository.GetAgvAsync(agvCorrelationId);

        if (!await _actionGroupRegistry.HasNotStartedActionsAsync(agvCorrelationId) || agv.IsLocked)
        {
            return;
        }
        try
        {
            var action = await _actionGroupRegistry.GetSubsequentActionAsync(agv.CorrelationId);
            _logger.LogDebug("Starting succeeding action {Action} of group {ActionGroup} for agv with CorrelationId: {AgvCorrelationId}.", action.CorrelationId, action.ParentId, agv.CorrelationId);
            await SelectNodesForAgvAction(context, action, agv, cancellationToken);
        }
        catch
        {
            await context.Publish(new StartingActionForAgvFailed(agvCorrelationId), cancellationToken);
        }
    }

    /// <summary>
    /// Handles the <see cref="TryStartNewActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryStartNewActionGroup> context)
    {
        var agvCorrelationId = context.Message.AgvCorrelationId;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvRepository.GetAgvAsync(agvCorrelationId);
    
        var agvSettings = _settingsProvider.GetSettings();
        bool needsCharging = agv.CheckNeedsCharging(agvSettings);
        bool isCarryingLoad = await _actionGroupRegistry.HasUnfinishedDropActionsAsync(agv.CorrelationId);

        bool canStartNewAction = !needsCharging && !agv.NeedsMaintenance && !isCarryingLoad && !agv.IsLocked;

        if (!canStartNewAction)
        {
            _logger.LogWarning("Can not start new action group for agv {Agv} (is locked {IsLocked}/ isCarryingLoad {IsCarryingLoad}/ needsCharging {NeedsCharging}).", agv.CorrelationId, agv.IsLocked, isCarryingLoad, needsCharging);
            return;
        }

        await _actionManagerSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var actionGroup = await _chooseAgvActionGroupService.GetNotStartedActionGroupAsync(agv, cancellationToken);
            if (actionGroup == null)
            {
                _logger.LogDebug("There is no next action available for agv with CorrelationId {AgvCorrelationId}.", agv.CorrelationId);
                await context.Publish(new AgvCanStartIdleAction(agv.CorrelationId), cancellationToken);
                return;
            }
            var action = actionGroup.Actions.First();
            _logger.LogDebug("Trying to start action {ActionId} with type {ActionType}.", action.CorrelationId, Enum.GetName(action.ActionType));
            await SelectNodesForAgvAction(context, action, agv, context.CancellationToken);
        }, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="CreateParkingActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateParkingActionGroup> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvRepository.GetAgvAsync(agvCorrelationId);
     
        var agvSettings = _settingsProvider.GetSettings();
        if (agv.CheckIsEligibleForCharging(agvSettings)|| agv.IsLocked)
        {
            return;
        }

        if (await _parkPointService.IsOnAParkNodeAsync(agv) || await _actionGroupRetrievalService.IsAgvHeadingToNodeOfType(agvCorrelationId, AgvGraph.NodeType.Park))
        {
            _logger.LogDebug("No need for a park action because the agv is already in park for agvId: {AgvCorrelationId}.", agv.CorrelationId);
            return;
        }
        _logger.LogInformation("Starting park action for agv with CorrelationId: {AgvCorrelationId}.", agv.CorrelationId);

        var parkNode = await _parkPointService.ReserveParkAsync(agv);

        var groupCorrelationId = Guid.NewGuid();
        var actionGroup = new ActionGroup(
            groupCorrelationId,
            string.Empty,
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
        var agvCorrelationId = context.Message.AgvCorrelationId;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvRepository.GetAgvAsync(agvCorrelationId);

        var agvSettings = _settingsProvider.GetSettings();
        bool needsCharging = agv.CheckNeedsCharging(agvSettings);
        bool isCarryingLoad = await _actionGroupRegistry.HasUnfinishedDropActionsAsync(agv.CorrelationId);

        bool shouldCharge = !isCarryingLoad && needsCharging;
       
        _logger.LogDebug("Evaluating {Name} with {NeedsCharging}, {IsCarryingLoad} for agv {CorrelationId}.", nameof(CreateChargingActionGroup), needsCharging, isCarryingLoad, agv.CorrelationId);

        if (!agv.CheckIsEligibleForCharging(agvSettings) || !shouldCharge || agv.IsLocked)
        {
            return;
        }

        if (agv.IsCharging || await _actionGroupRetrievalService.IsAgvHeadingToNodeOfType(agvCorrelationId, AgvGraph.NodeType.Charge))
        {
            _logger.LogDebug("No need for another charge action because the agv is already charging for agvId: {AgvId}.", agv.AgvId);
            // This might happen if the AssignAsync action is called several times due to a low SOC value, although the Agv is already charging
            return;
        }
        _logger.LogInformation("Starting charge action for agv with CorrelationId: {AgvCorrelationId}.", agv.CorrelationId);

        var chargingNode = await _chargingPointManager.ReserveChargerAsync(agv);

        var groupCorrelationId = Guid.NewGuid();
        var actionGroup = new ActionGroup(
            groupCorrelationId,
            string.Empty,
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
        var agvCorrelationId = context.Message.AgvCorrelationId;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvRepository.GetAgvAsync(agvCorrelationId);
  
        var agvSettings = _settingsProvider.GetSettings();
        bool needsCharging = agv.CheckNeedsCharging(agvSettings);
        bool isCarryingLoad = await _actionGroupRegistry.HasUnfinishedDropActionsAsync(agv.CorrelationId);

        bool shouldCharge = !isCarryingLoad && needsCharging;
        bool shouldDoMaintenance = !isCarryingLoad && agv.NeedsMaintenance;
        if (shouldCharge || !shouldDoMaintenance|| agv.IsLocked)
        {
            return;
        }

        _logger.LogInformation("Starting maintenance action for agv with CorrelationId: {AgvCorrelationId}.", agv.CorrelationId);

        var maintenanceNode = await _maintenanceService.GetMaintenanceNode(agv);

        var groupCorrelationId = Guid.NewGuid();
        var actionGroup = new ActionGroup(
            groupCorrelationId,
            string.Empty,
            actions: new List<AgvAction> {new(Guid.NewGuid(), ActionType.Move, maintenanceNode.Id, groupCorrelationId)},
            assignedAgv: agv.CorrelationId,
            creator: ActionGroupCreator.TransportOrder
        );

        await _actionGroupRegistry.AddAsync(actionGroup);
        await context.Publish(new ActionGroupCreatedForAgv(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), agv.CorrelationId), cancellationToken);
        await context.Publish(new ActionGroupCreated(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), actionGroup.CorrelationId), cancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="QueueActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<QueueActionGroup> context)
    {
        (var actionGroup, bool throwOnInvalid) = context.Message;
        var cancellationToken = context.CancellationToken;
     
        _logger.LogTrace("Validating ActionGroup with CorrelationId: {ActionGroupId}.", actionGroup.CorrelationId);

        var domainActionGroup = _mapper.Map<ActionGroup>(context.Message.ActionGroup);
        IEnumerable<ValidationFailure> validationFailures = await _actionGroupValidationService.ValidateAsync(domainActionGroup);
        bool isValid = !validationFailures.Any();

        if (!isValid)
        {
            _logger.LogError("Validation result of ActionGroup with CorrelationId: {ActionGroupId} is not valid! Errors: {Errors}", actionGroup.CorrelationId, validationFailures);
            if (throwOnInvalid)
            {
                throw new ValidationException(validationFailures);
            }
            domainActionGroup.InvalidateActionGroup();
        }
        Domain.Models.Agv.Agv? agv = null;

        if (isValid)
        {
            if (actionGroup.AssignedAgv != null)
            {
                var assignedAgv = await _agvRepository.GetAgvAsync(actionGroup.AssignedAgv!.Value);
                await SelectNodesForAgvAction(context, domainActionGroup.FirstNotStartedAction!, assignedAgv, cancellationToken);
                await context.RespondAsync(new Success());
                return;
            }

            var agvSettings = _settingsProvider.GetSettings();
            await _actionManagerSemaphoreProvider.ExecuteLockedAsync(async () =>
            {
                ICollection<Domain.Models.Agv.Agv> allReadyAgvs = await _agvRepository.GetReadyAgvsAsync();
                ICollection<Domain.Models.Agv.Agv> readyAgvs = allReadyAgvs.Where(a => !(a.CurrentAgvState == AgvState.Charging &&
                                                                                         a.AgvStatus!.BatteryCharge < agvSettings.BatteryLevelGood)).ToList();
                agv = await _chooseAgvActionGroupService.GetAgvAsync(domainActionGroup, readyAgvs.ToList(), cancellationToken);
            }, cancellationToken);
            if (agv != null)
            {
                domainActionGroup.Assign(agv);
            }
        }

        _logger.LogDebug("Adding ActionGroup with CorrelationId: {ActionGroupId}.", actionGroup.CorrelationId);
        await _actionGroupRegistry.AddAsync(domainActionGroup);
        var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(domainActionGroup);
        await context.Publish(new ActionGroupCreated(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);

        if (!isValid)
        {
            List<string> validationFailureReasons = validationFailures.Select(error => error.ErrorMessage).ToList();
            _logger.LogDebug("ActionGroup with CorrelationId: {ActionGroupId} is not valid, publishing ActionGroupInvalid.", actionGroup.CorrelationId);
            await context.Publish(new ActionGroupInvalid(actionGroup.CorrelationId, actionGroupMessageContent, validationFailureReasons), cancellationToken);
            return;
        }
     
        if (agv != null)
        {
            var action = domainActionGroup.FirstNotStartedAction;
            await SelectNodesForAgvAction(context, action!, agv, cancellationToken);
        }
    }

    /// <summary>
    /// Handles the <see cref="CompleteAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CompleteAction> context)
    {
        var correlationId = context.Message.ActionCorrelationId;
        var cancellationToken = context.CancellationToken;

        _logger.LogDebug("Completing action with CorrelationId: {ActionCorrelationId}.", correlationId);
        var action = await _actionGroupRegistry.GetRequiredActionByIdAsync(correlationId);

        var actionGroup = await _actionGroupRegistry.GetRequiredActionGroupByIdAsync(action.ParentId);
        if (actionGroup.AssignedAgv == null)
        {
            _logger.LogError("Failed to complete action {Action} for group {ActionGroup}, assigned agv is null.", action.CorrelationId, actionGroup.CorrelationId);
        }
        var agv = await _agvRepository.GetAgvAsync(actionGroup.AssignedAgv!.Value);
   
        actionGroup = await _actionGroupRegistry.UpdateActionAndGroupStateAsync(action, null, ActionState.Completed);
        var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);
        if (actionGroup.State == ActionGroupState.Completed)
        {
            await context.Publish(new ActionGroupCompleted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow), cancellationToken);
        }

        // charge will switch to charging state and can not be released immediately
        if (action.ActionType != ActionType.Charge)
        {
            await context.Publish(new NonChargingActionCompleted(agv.CorrelationId), cancellationToken);
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
            await context.PublishAndRespond(new ActionGroupCancelFailed(null!, actionGroupId, HostOrderCancelFailedErrorCode.OrderDoesNotExist));
          
            return;
        }
        var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);

        switch (actionGroup.State)
        {
            case ActionGroupState.Started:
                await context.PublishAndRespond(new ActionGroupCancelFailed(actionGroupMessageContent, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderAlreadyStarted));
                return;

            case ActionGroupState.Completed:
                await context.PublishAndRespond(new ActionGroupCancelFailed(actionGroupMessageContent, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderAlreadyFinished));
                return;

            case ActionGroupState.Failed:
                await context.PublishAndRespond(new ActionGroupCancelFailed(actionGroupMessageContent, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderAlreadyFailed));
                return;

            case ActionGroupState.Canceled:
                await context.PublishAndRespond(new ActionGroupCancelFailed(actionGroupMessageContent, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderAlreadyCancelled));
                return;
        }
       
        actionGroup = await _actionGroupRegistry.UpdateActionAndGroupStateAsync(actionGroup.FirstNotStartedAction!, null, ActionState.Canceled);
        await context.PublishAndRespond(new ActionGroupCancelled(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow));
    }

    private async Task SelectNodesForAgvAction(IPublishEndpoint context, AgvAction action, Domain.Models.Agv.Agv assignedAgv, CancellationToken cancellationToken)
    {
        if (!assignedAgv.TryTransitionToBusy(ignoreLocked: true))
        {
            _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} is not ready to be set to busy.", assignedAgv.CorrelationId);
            return;
        }

        // here the state has changed in all cases
        await PublishStateChange(context, assignedAgv);
        
        action.Nodes = (await _routeService.GetRouteAsync(assignedAgv, action.TargetNodeId, cancellationToken)).Nodes;

        _logger.LogDebug("Starting action with CorrelationId: {ActionCorrelationId} for Agv {AgvCorrelationId}. Setting Agv to busy in next step.", action.CorrelationId, assignedAgv.CorrelationId);
        try
        {
            if ((action.Nodes == null || !action.Nodes.Any()) && assignedAgv.AgvStatus!.LastNodeId != action.TargetNodeId)
            {
                throw new ArgumentNullException(nameof(action.Nodes));
            }
            await context.Publish(new NodesSelectedForAction(action, assignedAgv.CorrelationId), cancellationToken);
        }
        catch
        {
           _logger.LogWarning("Node selection failed for action: {Action}.", action);
        }
    }

    private async Task PublishStateChange(IPublishEndpoint context, Domain.Models.Agv.Agv agv)
    {
        var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);
        await context.Publish(new AgvStateChanged(agv.CorrelationId, agv.CurrentAgvState, agv.PreviousAgvState, agvMessageContent));
    }
}
