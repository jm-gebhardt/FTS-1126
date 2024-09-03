using AutoMapper;
using FluentValidation;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;
using MessageAgvAction = Gebhardt.Acc.Common.Messages.ActionGroups.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides handlers for <see cref="Action"/> related commands.
/// </summary>
[UsedImplicitly]
public class ActionCommandHandler :
    IConsumer<StartActionOfActionGroup>,
    IConsumer<TryStartManualAction>,
    IConsumer<TryStartNextActionInActionGroup>,
    IConsumer<TryStartNewActionGroup>,
    IConsumer<CreateParkingActionGroup>,
    IConsumer<CreateChargingActionGroup>,
    IConsumer<CompleteActionOfActionGroup>,
    IConsumer<QueueActionGroup>,
    IConsumer<CancelActionGroup>,
    IConsumer<SelectNodesForAction>,
    IConsumer<AbortActionGroup>,
    IConsumer<ResetActionGroup>,
    IConsumer<SetTargetForAction>,
    IConsumer<EstimateActionGroupDuration>,
    IConsumer<PauseActionGroup>,
    IConsumer<ResumeActionGroup>,
    IConsumer<CancelOrAbortActionGroup>
{
    private readonly ILogger<ActionCommandHandler> _logger;
    private readonly IRouteService _routeService;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IChooseAgvActionGroupService _chooseAgvActionGroupService;
    private readonly ISemaphoreProvider<IAgvsSemaphore> _actionManagerSemaphoreProvider;
    private readonly IMapper _mapper;
    private readonly IBus _bus;
    private readonly IActionGroupValidationService _actionGroupValidationService;
    private readonly IActionGroupRetrievalService _actionGroupRetrievalService;
    private readonly IAgvDb _agvDb;
    private readonly IActionGroupDurationEstimationService _durationEstimationService;
    private readonly IMetricCollectionService _metricCollectionService;
    private readonly ActionCommandHelper _actionService;

    /// <inheritdoc cref="ActionCommandHandler"/>
    public ActionCommandHandler(
        IRouteService routeService,
        IActionGroupRepository actionGroupRepository,
        IActionGroupValidationService actionGroupValidationService,
        IChooseAgvActionGroupService chooseAgvActionGroupService,
        IActionGroupRetrievalService actionGroupRetrievalService,
        ISemaphoreProvider<IAgvsSemaphore> actionManagerSemaphoreProvider,
        IActionGroupDurationEstimationService durationEstimationService,
        IAgvDb agvDb,
        IMetricCollectionService metricCollectionService,
        IMapper mapper,
        IBus bus,
        ILogger<ActionCommandHandler> logger,
        ActionCommandHelper actionService)
    {
        _actionService = actionService;
        _logger = logger;
        _routeService = routeService;
        _agvDb = agvDb;
        _actionGroupRepository = actionGroupRepository;
        _chooseAgvActionGroupService = chooseAgvActionGroupService;
        _actionManagerSemaphoreProvider = actionManagerSemaphoreProvider;
        _actionGroupValidationService = actionGroupValidationService;
        _actionGroupRetrievalService = actionGroupRetrievalService;
        _durationEstimationService = durationEstimationService;
        _metricCollectionService = metricCollectionService;
        _mapper = mapper;
        _bus = bus;
    }

    /// <summary>
    /// Handles the <see cref="StartActionOfActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<StartActionOfActionGroup> context)
    {
        var (action, agvCorrelationId) = context.Message;
        var cancellationToken = context.CancellationToken;

        await _actionManagerSemaphoreProvider.ExecuteLockedAsync(async () =>
        { 
            var assignedAgv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
            var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByActionIdAsync(action.CorrelationId);
            bool agvActionCanBeStarted = await _actionService.CheckAgvActionCanBeStarted(context, action, assignedAgv, actionGroup);
            if (agvActionCanBeStarted)
            {
                actionGroup.StartAction(action.CorrelationId, action.Nodes, action.Edges);
                await _actionService.TryUpdateActionGroup(actionGroup);
                var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
                await context.Publish(new StartActionCommand(assignedAgv.CorrelationId, assignedAgv.SerialNumber, _mapper.Map<MessageAgvAction>(action)), cancellationToken);
                await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);
                if (action.IsFirstAction)
                {
                    _logger.LogOrderInformation(
                        LogCodes.OrderStarted, 
                        actionGroup.AssignedAgv.GetValueOrDefault(), 
                        actionGroup.CorrelationId, 
                        "Order Started", 
                        $"Order {actionGroup.OrderId} has been started");
                    await context.Publish(new ActionGroupStarted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow), cancellationToken);
                }
                _logger.LogOrderInformation(
                    LogCodes.OrderActionStarted, 
                    actionGroup.AssignedAgv.GetValueOrDefault(), 
                    actionGroup.CorrelationId, 
                    "Order Updated", 
                    $"{action.ActionType} of order {actionGroup.OrderId} has been started");
                _logger.LogInformation("{ActionType} for action group {CorrelationId} has started.", action.ActionType, actionGroup.CorrelationId);
                await _actionService.LogStationUpdate(assignedAgv, action, actionGroup);
            }
        }, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="SelectNodesForAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SelectNodesForAction> context)
    {
        var (actionMessageContent, assignedAgvId) = context.Message;
        var cancellationToken = context.CancellationToken;
        var assignedAgv = await _agvDb.Agvs.GetAgvAsync(assignedAgvId);

        var actionGroup = await _actionGroupRepository.GetActionGroupAsync(actionMessageContent.ParentId);

        var action = actionGroup?.FirstNotStartedAction;

        if (action == null)
        {
            _logger.LogError("Could not find un-started action for {ActionGroupCorrelationId} and assigned agv {AgvCorrelationId}.", actionMessageContent.ParentId, assignedAgvId);
            return;
        }
        _logger.LogDebug("Trying to start action {ActionId} with type {ActionType} of group {ActionGroupCorrelationId} for agv {AgvCorrelationId}.", action.CorrelationId, Enum.GetName(action.ActionType), actionGroup!.CorrelationId, assignedAgvId);

        if (actionMessageContent.CorrelationId != action.CorrelationId)
        {
            _logger.LogWarning("Action in message for which nodes are to be selected {ActionInMessage} is not the first not started action {NotStartedAction} of the group {ActionGroupCorrelationId}", actionMessageContent.CorrelationId, action.CorrelationId, actionGroup.CorrelationId);
        }

        if (assignedAgv.CorrelationId != actionGroup.AssignedAgv!)
        {
            _logger.LogWarning("Agv in message for which nodes are to be selected {AgvInMessage} is not the agv {AgvCorrelationId} assigned to the group {ActionGroupCorrelationId}", assignedAgvId, actionGroup.AssignedAgv, actionGroup.CorrelationId);
        }

        if (await _actionService.SelectNodesForAgvAction(action, assignedAgv, false, cancellationToken))
        {
            await context.Publish(new NodesSelectedForAction(action, assignedAgv.CorrelationId), cancellationToken);
        }
    }

    /// <summary>
    /// Handles the <see cref="EstimateActionGroupDuration"/> command.
    /// </summary>    
    public async Task Consume(ConsumeContext<EstimateActionGroupDuration> context)
    {
        var agvId = context.Message.AgvId;
        var action = context.Message.Action;
        var actionGroupId = context.Message.ActionGroupId;

        if (!action.IsFirstAction)
        {
            // The duration has already been estimated previously.
            await context.Publish(new ActionDurationEstimated(action, agvId, actionGroupId));
            return;
        }

        Task<Domain.Models.ActionGroup.ActionGroup> actionGroupTask = _actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupId);
        Task<Domain.Models.Agv.Agv> agvTask = _agvDb.Agvs.GetAgvAsync(agvId);
        Task<AgvData?> agvDataTask = _agvDb.Data.GetAgvDataAsync(agvId);
        await Task.WhenAll(actionGroupTask, agvTask, agvDataTask);
        var actionGroup = actionGroupTask.Result;
        var assignedAgv = agvTask.Result;
        var agvData = agvDataTask.Result;

        if (actionGroup.AssignedAgv.GetValueOrDefault() != agvId)
        {
            _logger.LogDebug("Action group {ActionGroupCorrelationId} is already assigned to another agv {AgvCorrelationId}/{OtherAgv}", actionGroup.CorrelationId, actionGroup.AssignedAgv, agvId);
            return;
        }
        double duration = 0;
        bool lastActionWasMoveToCoordinate = await _actionGroupRepository.IsLastCompletedActionMoveToCoordinate(assignedAgv.CorrelationId);
        if (!lastActionWasMoveToCoordinate && !action.IsMoveToCoordinate)
        {
            var route = await _routeService.GetRouteAsync(assignedAgv, actionGroup.GetTargets(), null, agvData);
            duration = route.Costs;

            // average of unaccounted for costs (e.g. cargo loading and unloading) for pick/drop actions
            if (actionGroup.IsMovingCargo)
            {
                duration += await _durationEstimationService.GetAverageDurationEstimateDeviationAsync(actionGroup.AssignedAgv!.Value, actionGroup.GetTargets());
            }
        }

        if (lastActionWasMoveToCoordinate || action.IsMoveToCoordinate)
        {
            duration += await _durationEstimationService.GetDurationEstimate(agvData!.MaxVelocityStraight, actionGroup.Actions.First(), assignedAgv.CorrelationId);
        }
        // costs for charging itself, just assume 1% / s, we don't have the charging rate here
        double chargeCostsPerCharge = 100 - assignedAgv.AgvStatus!.BatteryCharge;

        duration += actionGroup.Actions.Count(a => a.ActionType == ActionType.Charge) * chargeCostsPerCharge;

        // costs for action switching / state transitions of the Agv from one action to the next action
        duration += actionGroup.Actions.Count * ActionCosts.SwitchingActions;

        actionGroup.EstimatedDuration = TimeSpan.FromSeconds(duration);

        await _actionService.TryUpdateActionGroup(actionGroup);
        await context.Publish(new ActionDurationEstimated(action, agvId, actionGroupId));
        _logger.LogDebug("Estimated duration for action group {ActionGroupCorrelationId} and agv {AgvCorrelationId}: {EstimatedDuration}", actionGroup.CorrelationId, assignedAgv.CorrelationId, actionGroup.EstimatedDuration);
    }

    /// <summary>
    /// Handles the <see cref="TryStartManualAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryStartManualAction> context)
    {
        var agvCorrelationId = context.Message.AgvCorrelationId;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

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
            _logger.LogDebug("There is no manual action group available for agv with CorrelationId {AgvCorrelationId}.", agv.CorrelationId);
            return;
        }
        var action = actionGroup.FirstNotStartedAction;
        if (action == null)
        {
            _logger.LogDebug("There is no un-started manual action available for agv with CorrelationId {AgvCorrelationId}.", agv.CorrelationId);
            return;
        }
        if (await _actionService.SelectNodesForAgvAction(action, agv, false, context.CancellationToken))
        {
            await context.Publish(new NodesSelectedForAction(action, agv.CorrelationId), context.CancellationToken);
        }
    }

    /// <summary>
    /// Handles the <see cref="TryStartNextActionInActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryStartNextActionInActionGroup> context)
    {
        var agvCorrelationId = context.Message.AgvCorrelationId;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        if (!await _actionGroupRepository.HasNotStartedActionsAsync(agvCorrelationId))
        {
            _logger.LogWarning("No succeeding action  for agv with CorrelationId: {AgvCorrelationId}.", agv.CorrelationId);
            return;
        }
        try
        {
            var action = await _actionGroupRepository.GetSubsequentActionAsync(agv.CorrelationId);
            _logger.LogDebug("Starting succeeding action {Action} of group {ActionGroupCorrelationId} for agv with CorrelationId: {AgvCorrelationId}.", action.CorrelationId, action.ParentId, agv.CorrelationId);
            if (action.IsIncomplete)
            {
                _logger.LogWarning("Succeeding action {Action} of group {ActionGroupCorrelationId} is incomplete, thus not started for agv with CorrelationId: {AgvCorrelationId}.", action.CorrelationId, action.ParentId, agv.CorrelationId);
                await context.Publish(new StartingActionForAgvFailed(agvCorrelationId), cancellationToken);
                return;
            }

            if(action.State == ActionState.Paused){
                _logger.LogWarning("Succeeding action {Action} of group {ActionGroupCorrelationId} is paused, thus not started for agv with CorrelationId: {AgvCorrelationId}.", action.CorrelationId, action.ParentId, agv.CorrelationId);
                return;
            }

            if (!await _actionService.SelectNodesForAgvAction(action, agv, true, cancellationToken))
            {
                await context.Publish(new StartingActionForAgvFailed(agvCorrelationId), cancellationToken);
                return;
            }
            await context.Publish(new NodesSelectedForAction(action, agv.CorrelationId), cancellationToken);
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
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        // When checking to see if a new action group can be started, make sure to check the battery levels of the AGV. Do not
        // proceed if the AGV is in critical condition unless there is already existing charging action group because that will be
        // picked and prioritized in _chooseAgvActionGroupService. This is required because the chain of events that create a
        // charging action group might miss a window to start charging before the next action group is assigned to an idle AGV and
        // the AGV battery dies before moving to the charger is complete.
        var agvSettings = _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
        bool agvMustCharge = agv.CheckMustCharge(agvSettings);
        bool hasChargingActionGroup = await _actionGroupRepository.HasCreatedChargingActionGroupForAgvAsync(agvCorrelationId);
        bool hasUnfinishedDropAction = await _actionGroupRepository.HasUnfinishedDropActionsAsync(agv.CorrelationId);
        bool canStartNewAction = !agv.NeedsMaintenance && !hasUnfinishedDropAction && !agv.IsLocked && (!agvMustCharge || hasChargingActionGroup);
        if (!canStartNewAction)
        {
            _logger.LogWarning(
                "Can not start new action group for AGV {AgvCorrelationId} (is locked: {IsLocked}/ has unfinished drop action: {HasUnfinishedDropAction}/ needs maintenance: {NeedsMaintenance}/ AGV must charge: {AgvMustCharge}/ AGV has charging action group: {HasChargingActionGroup}).", 
                agv.CorrelationId, 
                agv.IsLocked, 
                hasUnfinishedDropAction, 
                agv.NeedsMaintenance,
                agvMustCharge,
                hasChargingActionGroup);
            return;
        }

        await _actionManagerSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var agvData = await _agvDb.Data.GetAgvDataAsync(agv.CorrelationId);
            var chosenActionGroup = await _chooseAgvActionGroupService.GetNotStartedActionGroupAsync(agv, agvData, cancellationToken);

            if (chosenActionGroup == null)
            {
                _logger.LogDebug("There is no next action available for agv with CorrelationId {AgvCorrelationId}.", agv.CorrelationId);
                await context.Publish(new AgvCanStartIdleAction(agv.CorrelationId), cancellationToken);
                return;
            }

            var action = chosenActionGroup.FirstNotStartedAction!;
            _logger.LogDebug("Trying to start action {ActionId} of group {ActionGroupCorrelationId} with type {ActionType}", action.CorrelationId, action.ParentId, Enum.GetName(action.ActionType));

            if (action.IsIncomplete)
            {
                _logger.LogWarning("Could not start incomplete action {ActionId} of group {ActionGroupCorrelationId} with type {ActionType}", action.CorrelationId, action.ParentId, Enum.GetName(action.ActionType));
                return;
            }

            if (await _actionService.SelectNodesForAgvAction(action, agv, false, context.CancellationToken))
            {
                var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(chosenActionGroup.CorrelationId);
                actionGroup.Assign(agv);
                _logger.LogOrderInformation(LogCodes.OrderAgvAssigned, agv.CorrelationId, actionGroup.CorrelationId, "Agv was assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}");

                await _actionService.TryUpdateActionGroup(actionGroup);
                await context.Publish(new NodesSelectedForAction(action, agv.CorrelationId), cancellationToken);
            }
        }, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="CreateParkingActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateParkingActionGroup> context)
    {
        (var groupCorrelationId, var agvCorrelationId, string parkNode) = context.Message;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        var actionGroup = new Domain.Models.ActionGroup.ActionGroup(
            groupCorrelationId,
            string.Empty,
            actions: new List<AgvAction> { new(Guid.NewGuid(), ActionType.Move, parkNode, groupCorrelationId) },
            assignedAgv: agv.CorrelationId,
            creator: ActionGroupCreator.TransportOrder,
            hasPriority: false
        );
        _logger.LogOrderInformation(LogCodes.OrderCreated, agv.CorrelationId, actionGroup.CorrelationId, "Order created", $"Order {actionGroup.OrderId} was created successfully");
        _logger.LogOrderInformation(LogCodes.OrderAgvAssigned, agv.CorrelationId, actionGroup.CorrelationId, "Agv was assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}");

        await _actionGroupRepository.AddAsync(actionGroup);
        await context.Publish(new ActionGroupCreated(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId), cancellationToken);
        await context.Publish(new ActionGroupCreatedForAgv(_mapper.Map<ActionGroup>(actionGroup), agv.CorrelationId), cancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="CreateChargingActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateChargingActionGroup> context)
    {
        (var groupCorrelationId, var agvCorrelationId, string chargeStationId, var initiator) = context.Message;
        var cancellationToken = context.CancellationToken;

        bool triggeredByWebApi = initiator == ActionGroupCreator.TransportOrderWebApi;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        if (agv.IsLocked || (!agv.CanStartAutomaticAction && !triggeredByWebApi))
        {
            _logger.LogInformation("Could not create charging action group for AGV:{CorrelationId}. locked:{IsLocked}, CanStartAutomaticAction:{CanStartAutomaticAction}, TriggeredByWebApi:{TriggeredByWebApi}.",
                agv.CorrelationId,
                agv.IsLocked,
                agv.CanStartAutomaticAction,
                triggeredByWebApi);
            return;
        }

        if (agv.IsCharging)
        {
            _logger.LogInformation("AGV: {SerialNumber}/{AgvCorrelationId} is already charging", agv.SerialNumber, agv.CorrelationId);
            // This might happen if the AssignAsync action is called several times due to a low SOC value, although the agv is already charging
            return;
        }

        if (await _actionGroupRetrievalService.IsAgvHeadingToNodeOfType(agvCorrelationId, AgvGraph.NodeType.Charge))
        {
            _logger.LogInformation("AGV:{SerialNumber}/{AgvCorrelationId} is already heading to a charging station.", agv.SerialNumber, agv.CorrelationId);
            // This might happen if the AssignAsync action is called several times due to a low SOC value, although the agv is already charging
            return;
        }

        IReadOnlyList<Domain.Models.ActionGroup.ActionGroup> actionGroups = await _actionGroupRepository.GetCreatedAndStartedActionGroupsForAgvAsync(agvCorrelationId);

        var agvSettings = _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
        bool agvMustCharge = agv.CheckMustCharge(agvSettings);
        bool hasNotStartedActionGroups = actionGroups.Any();

        var startedChargingAction = actionGroups
            .FirstOrDefault(ag => ag.State == ActionGroupState.Started && ag.Actions.Any(a => a.ActionType == ActionType.Charge));
        if(startedChargingAction != null)
        {
            _logger.LogInformation("AGV:{CorrelationID} has already started an action group for charging.", agv.CorrelationId);
            return;
        }
        
        if(!agvMustCharge && hasNotStartedActionGroups)
        {
            _logger.LogInformation("Skip charging for AGV:{CorrelationID}. Must charge: {AgvMustCharge}. Action groups not started: {HasNotStartedActionGroups}",
                agv.CorrelationId,
                agvMustCharge,
                hasNotStartedActionGroups);
            return;
        }

        // Yes, we want to charge
        _logger.LogInformation("Creating charge action for agv with CorrelationId: {AgvCorrelationId}.", agv.CorrelationId);
 
        // Let's see if there is a pending charge action group
        var actionGroup = actionGroups.FirstOrDefault(a => a.Actions.Any(ac => ac.ActionType == ActionType.Charge));
        if (actionGroup == null)
        {
            actionGroup = new Domain.Models.ActionGroup.ActionGroup(
                groupCorrelationId,
                string.Empty,
                actions: new List<AgvAction>
                {
                    new(Guid.NewGuid(), ActionType.Move, chargeStationId, groupCorrelationId)
                    {
                        Sequence = 0
                    },
                    new(Guid.NewGuid(), ActionType.Charge, chargeStationId, groupCorrelationId)
                    {
                        Sequence = 1
                    }
                },
                assignedAgv: agv.CorrelationId,
                creator: initiator,
                isManual: triggeredByWebApi,
                hasPriority: triggeredByWebApi
            );

            await _actionGroupRepository.AddAsync(actionGroup);
            await context.Publish(new ActionGroupCreated(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId), cancellationToken);
            _logger.LogOrderInformation(LogCodes.OrderCreated, agv.CorrelationId, actionGroup.CorrelationId, "Order created", $"Order {actionGroup.OrderId} was created successfully");
            _logger.LogOrderInformation(LogCodes.OrderAgvAssigned, agv.CorrelationId, actionGroup.CorrelationId, "Agv was assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}");
        }

        await context.Publish(new ActionGroupCreatedForAgv(_mapper.Map<ActionGroup>(actionGroup), agv.CorrelationId), cancellationToken);   
    }

    /// <summary>
    /// Handles the <see cref="QueueActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<QueueActionGroup> context)
    {
        (var actionGroup, bool throwOnInvalid) = context.Message;
        var cancellationToken = context.CancellationToken;

        _logger.LogTrace("Validating ActionGroup with CorrelationId: {ActionGroupCorrelationId}.", actionGroup.CorrelationId);

        var domainActionGroup = _mapper.Map<Domain.Models.ActionGroup.ActionGroup>(context.Message.ActionGroup);
        var validationResult = await _actionGroupValidationService.ValidateAsync(domainActionGroup);
        bool isValid = validationResult.IsValid;

        if (!isValid)
        {
            _logger.LogError("Validation result of ActionGroup with CorrelationId: {ActionGroupCorrelationId} is not valid! Errors: {Errors}", actionGroup.CorrelationId, validationResult);
            if (throwOnInvalid)
            {
                throw new ValidationException(validationResult.Errors);
            }
            domainActionGroup.InvalidateActionGroup(validationResult.ToReasons());
        }
        Domain.Models.Agv.Agv? agv = null;

        if (isValid)
        {
            agv = await _actionService.GetAgvForActionGroup(cancellationToken, actionGroup.AssignedAgv, domainActionGroup);
            if (actionGroup.AssignedAgv != null)
            {
                await context.RespondAsync(new Success());
            }
        }

        await _actionGroupRepository.AddAsync(domainActionGroup);
        _logger.LogOrderInformation(LogCodes.OrderCreated, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Order created", $"Order {actionGroup.OrderId} was created successfully");

        var actionGroupMessageContent = _mapper.Map<ActionGroup>(domainActionGroup);
        await _bus.Publish(new ActionGroupCreated(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);

        if (!isValid)
        {
            List<string> validationFailureReasons = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            _logger.LogOrderWarning(LogCodes.OrderInvalid, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Order Invalid", $"Order {actionGroup.OrderId} is invalid due to {string.Join(",", validationFailureReasons)}");

            await _bus.Publish(new ActionGroupInvalid(actionGroup.CorrelationId, actionGroupMessageContent, validationFailureReasons), cancellationToken);
            return;
        }

        if (agv != null && !domainActionGroup.FirstNotStartedAction!.IsIncomplete)
        {
            _logger.LogOrderInformation(LogCodes.OrderAgvAssigned, agv.CorrelationId, actionGroup.CorrelationId, "Agv was assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}");

            var agvSettings = _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
            bool agvMustCharge = agv.CheckMustCharge(agvSettings);

            // Do not publish ActionGroupCreatedForAgv if we must charge. ActionGroupCreatedForAgv will trigger
            // a chain of events resulting in attempting to start this ActionGroup.
            if(!agvMustCharge)
            {
                await context.Publish(new ActionGroupCreatedForAgv(actionGroupMessageContent, agv.CorrelationId), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Handles the <see cref="CompleteActionOfActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CompleteActionOfActionGroup> context)
    {
        var correlationId = context.Message.ActionCorrelationId;
        var cancellationToken = context.CancellationToken;

        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByActionIdAsync(correlationId);
        var agvCorrelationId = actionGroup.AssignedAgv!.Value;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        var action = actionGroup.Actions.Single(a => a.CorrelationId == correlationId);

        actionGroup.CompleteAction(correlationId);
        await _actionService.TryUpdateActionGroup(actionGroup);

        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);
        await context.Publish(new NonChargingActionCompleted(agvCorrelationId), cancellationToken);

        if (!agv.CanStartAutomaticAction && !actionGroup.IsManual && actionGroup.State != ActionGroupState.Paused)
        {
            await context.Publish(new PauseActionGroup(actionGroup.CorrelationId), cancellationToken);
        }

        if (action.ActionType == ActionType.Pick)
        {
            await context.Publish(new PickActionCompleted(actionGroup.CorrelationId, action.CorrelationId, action.CompletedDate.GetValueOrDefault()), cancellationToken);
        }

        if (actionGroup.State != ActionGroupState.Completed)
        {
            _logger.LogOrderInformation(LogCodes.OrderActionCompleted, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Order Updated", $"{actionGroup.Actions.First(a => a.CorrelationId == correlationId).ActionType} of order {actionGroup.OrderId} has been completed");
            return;
        }

        await context.Publish(new ActionGroupCompleted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow), cancellationToken);
        _logger.LogOrderInformation(LogCodes.OrderCompleted, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Order Completed", $"Order {actionGroup.OrderId} was completed successfully");
        await _metricCollectionService.CollectOrderCompletionMetrics(actionGroup);
    }

    /// <summary>
    /// Handles the <see cref="AbortActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AbortActionGroup> context)
    {
        var correlationId = context.Message.CorrelationId;

        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(correlationId);

        if (actionGroup.AssignedAgv == null)
        {
            _logger.LogError("Could not abort action group {ActionGroupCorrelationId}, it is not assigned to an agv.", actionGroup.CorrelationId);
            await context.RespondAsync(new AbortActionGroupFailed("Action group abortion failed, no assigned agv."));
            return;
        }
        var action = actionGroup.Actions.FirstOrDefault(a => a.State != ActionState.Completed);
        if (action == null)
        {
            _logger.LogError("Could not abort action group {ActionGroupCorrelationId}, no actions that are not completed", actionGroup.CorrelationId);
            await context.RespondAsync(new AbortActionGroupFailed("Action group abortion failed, all actions completed."));
            return;
        }
        var agv = await _agvDb.Agvs.GetAgvAsync((Guid)actionGroup.AssignedAgv!);
        actionGroup = await _actionGroupRepository.UpdateActionAndGroupStateAsync(action, agv, ActionState.Aborted, context.Message.AbortReason.ToReason());

        await context.Publish(new ActionAborted(agv.CorrelationId, _mapper.Map<MessageAgvAction>(action)));
        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));
        await context.RespondAsync(new Success());
        _logger.LogOrderWarning(LogCodes.OrderAborted, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Order Aborted", $"Order {actionGroup.OrderId} was aborted due to {context.Message.AbortReason}");

        var actionGroupWithNodes = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(actionGroup);
        if (actionGroupWithNodes.Actions.Any(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)))
        {
            string serviceNode = actionGroupWithNodes.Actions.First(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)).TargetNodeId;
            await context.Publish(new AutomaticActionWithdrawn(serviceNode));
        }
        await _metricCollectionService.CollectOrderCompletionMetrics(actionGroup, agv.AgvStatus!.LastNodeId);
    }

    /// <summary>
    /// Handles the <see cref="CancelActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CancelActionGroup> context)
    {
        var actionGroupId = context.Message.CorrelationId;
        var actionGroup = await _actionGroupRepository.GetActionGroupAsync(actionGroupId);

        if (actionGroup == null)
        {
            await context.PublishAndRespond(new ActionGroupCancelFailed(null!, actionGroupId, HostOrderCancelFailedErrorCode.OrderDoesNotExist));

            return;
        }
        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);

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
                await context.PublishAndRespond(new ActionGroupCancelFailed(actionGroupMessageContent, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderAlreadyCanceled));
                return;

            case ActionGroupState.Aborted:
                await context.Publish(new ActionGroupCancelFailed(actionGroupMessageContent, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderAlreadyCanceled));
                return;
        }

        actionGroup = await _actionGroupRepository.UpdateActionAndGroupStateAsync(actionGroup.FirstNotStartedAction ?? actionGroup.Actions.First(), null, ActionState.Canceled, context.Message.CancelReason.ToReason());
        await context.PublishAndRespond(new ActionGroupCanceled(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow));
        _logger.LogOrderWarning(LogCodes.OrderCanceled, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Order Canceled", $"Order {actionGroup.OrderId} was canceled due to {context.Message.CancelReason}");

        var actionGroupWithNodes = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(actionGroup);
        if (actionGroupWithNodes.Actions.Any(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)))
        {
            string serviceNode = actionGroupWithNodes.Actions.First(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)).TargetNodeId;
            await context.Publish(new AutomaticActionWithdrawn(serviceNode));
        }

        actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="ResetActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResetActionGroup> context)
    {
        var correlationId = context.Message.CorrelationId;
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(correlationId);
        Guid? agvCorrelationId = actionGroup.AssignedAgv;
        var action = actionGroup.Actions.FirstOrDefault(a => a.State == ActionState.Started);
        if (action != null && agvCorrelationId != null)
        {
            await context.Publish(new ActionAborted(agvCorrelationId.Value, _mapper.Map<MessageAgvAction>(action)));
        }

        actionGroup.ResetActionGroup();
        await _actionService.TryUpdateActionGroup(actionGroup);
        var actionGroupWithNodes = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(actionGroup);
        if (actionGroupWithNodes.Actions.Any(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)))
        {
            string serviceNode = actionGroupWithNodes.Actions.First(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)).TargetNodeId;
            await context.Publish(new AutomaticActionWithdrawn(serviceNode));
        }
        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="PauseActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<PauseActionGroup> context)
    {
        var actionGroupId = context.Message.CorrelationId;
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupId);

        if (actionGroup.State != ActionGroupState.Started)
        {
            _logger.LogWarning("Can not pause action group {ActionGroupId} because it is in state {ActionGroupState}.", actionGroupId, actionGroup.State);
            return;
        }

        var activeAction = actionGroup.FirstNotCompletedAction;
        ActionState? actionState = activeAction?.State;

        actionGroup.PauseActionGroup();
        await _actionService.TryUpdateActionGroup(actionGroup);

        if (actionState == ActionState.Started && activeAction != null)
        {
            await context.Publish(new ActionAborted(activeAction.CorrelationId, _mapper.Map<MessageAgvAction>(activeAction)));
        }

        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));

        _logger.LogOrderInformation(LogCodes.OrderPaused, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroupId, "Order paused", $"Order {actionGroup.OrderId} has been paused");
    }

    /// <summary>
    /// Handles the <see cref="ResumeActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResumeActionGroup> context)
    {
        var actionGroupId = context.Message.CorrelationId;
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupId);

        if (actionGroup.State != ActionGroupState.Paused)
        {
            _logger.LogWarning("Can not resume action group {ActionGroupId} because it is in state {ActionGroupState}.", actionGroupId, actionGroup.State);
            return;
        }

        actionGroup.ResumeActionGroup();
        await _actionService.TryUpdateActionGroup(actionGroup);

        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));

        _logger.LogOrderInformation(LogCodes.OrderResumed, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroupId, "Order resumed", $"Order {actionGroup.OrderId} has been resumed");

        await context.Publish(new ActionGroupResumed(actionGroup.CorrelationId, actionGroup.AssignedAgv!.Value, context.Message.Setter));
    }

    /// <summary>
    /// Handles the <see cref="SetTargetForAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SetTargetForAction> context)
    {
        (var actionId, var actionGroupId, Guid? assignedAgv, string targetNodeId, _) = context.Message;

        var cancellationToken = context.CancellationToken;

        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupId);

        var action = actionGroup.Actions.First(a => a.CorrelationId == actionId);

        actionGroup.SetTargetForAction(actionId, targetNodeId);
        await _actionService.GetAgvForActionGroup(cancellationToken, assignedAgv, actionGroup);
        await _actionGroupRepository.UpdateAsync(actionGroup);

        await context.Publish(new TargetForActionSet(_mapper.Map<Common.Messages.ActionGroups.AgvAction>(actionGroup.FirstNotStartedAction!), actionGroup.AssignedAgv), cancellationToken);
        _logger.LogOrderInformation(LogCodes.OrderIncomplete, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Action target set", $"Target {targetNodeId} set for action {action.ActionType.ToString()}");
    }

    /// <summary>
    /// Handles the <see cref="CancelOrAbortActionGroup"/>
    /// </summary>
    public async Task Consume(ConsumeContext<CancelOrAbortActionGroup> context)
    {
        var actionGroupId = context.Message.CorrelationId;
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByActionIdAsync(actionGroupId);
        if (actionGroup.AssignedAgv == null)
        {
            await context.Publish(new CancelActionGroup(actionGroupId, CancelReason.External));
            return;
        }
        await context.Publish(new AbortActionGroup(actionGroupId, AbortReason.External));
    }
}