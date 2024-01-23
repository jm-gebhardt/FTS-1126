using AutoMapper;
using FluentValidation;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvAction = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup.AgvAction;
using AgvSettings = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings.AgvSettings;
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
    IConsumer<StartChargingActionGroup>,
    IConsumer<CompleteAction>,
    IConsumer<QueueActionGroup>,
    IConsumer<CancelActionGroup>,
    IConsumer<SelectNodesForAction>,
    IConsumer<AbortActionGroup>,
    IConsumer<ResetActionGroup>
{
    private readonly ILogger<ActionCommandHandler> _logger;
    private readonly IRouteService _routeService;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IChooseAgvActionGroupService _chooseAgvActionGroupService;
    private readonly ISemaphoreProvider<IAgvsSemaphore> _actionManagerSemaphoreProvider;
    private readonly IChargingPointManager _chargingPointManager;
    private readonly IParkPointService _parkPointService;
    private readonly ISettingsProvider<AgvSettings> _settingsProvider;
    private readonly IMapper _mapper;
    private readonly IBus _bus;
    private readonly IActionGroupValidationService _actionGroupValidationService;
    private readonly IActionGroupRetrievalService _actionGroupRetrievalService;
    private readonly INodeStateRepository _nodeStateRepository;
    private readonly IAgvDb _agvDb;

    /// <inheritdoc cref="ActionCommandHandler"/>
    public ActionCommandHandler(
        IChargingPointManager chargingPointManager,
        IParkPointService parkPointService,
        IRouteService routeService,
        IActionGroupRepository actionGroupRepository,
        IActionGroupValidationService actionGroupValidationService,
        IChooseAgvActionGroupService chooseAgvActionGroupService,
        IActionGroupRetrievalService actionGroupRetrievalService,
        ISemaphoreProvider<IAgvsSemaphore> actionManagerSemaphoreProvider,
        INodeStateRepository nodeStateRepository,
        IAgvDb agvDb,
        ISettingsProvider<AgvSettings> settingsProvider,
        IMapper mapper,
        IBus bus,
        ILogger<ActionCommandHandler> logger)
    {
        _logger = logger;
        _routeService = routeService;
        _agvDb = agvDb;
        _actionGroupRepository = actionGroupRepository;
        _chooseAgvActionGroupService = chooseAgvActionGroupService;
        _actionManagerSemaphoreProvider = actionManagerSemaphoreProvider;
        _parkPointService = parkPointService;
        _chargingPointManager = chargingPointManager;
        _actionGroupValidationService = actionGroupValidationService;
        _actionGroupRetrievalService = actionGroupRetrievalService;
        _nodeStateRepository = nodeStateRepository;
        _settingsProvider = settingsProvider;
        _mapper = mapper;
        _bus = bus;
    }

    /// <summary>
    /// Handles the <see cref="StartAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<StartAction> context)
    {
        var (action, agvCorrelationId) = context.Message;
        var cancellationToken = context.CancellationToken;
        var assignedAgv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        bool agvActionCanBeStarted = await CheckAgvActionCanBeStarted(context, action, assignedAgv);
        if (agvActionCanBeStarted)
        {
            var actionGroup = await _actionGroupRepository.UpdateActionAndGroupStateAsync(action, assignedAgv, ActionState.Started);
            var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
            await context.Publish(new StartActionCommand(assignedAgv.CorrelationId, assignedAgv.AgvId, _mapper.Map<MessageAgvAction>(action)), cancellationToken);
            await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);

            if (action.IsFirstAction)
            {
                await context.Publish(new ActionGroupStarted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow), cancellationToken);
            }
            _logger.LogDebug("Action with CorrelationId: {ActionCorrelationId} for Agv {AgvCorrelationId} has been started.", action.CorrelationId, assignedAgv.CorrelationId);
        }
    }

    private async Task<bool> CheckAgvActionCanBeStarted(IPublishEndpoint context, AgvAction action, Domain.Models.Agv.Agv assignedAgv)
    {
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(action.ParentId);
        bool isNextActionInGroup = actionGroup.FirstNotStartedAction!.CorrelationId == action.CorrelationId;
        if (!isNextActionInGroup)
        {
            _logger.LogWarning("Preventing start of action {Action} of group {Group}, because action another should start first.", action.CorrelationId, actionGroup.CorrelationId);
            return false;
        }

        IReadOnlyList<Domain.Models.ActionGroup.ActionGroup> startedActionGroups = await _actionGroupRepository.GetActionGroupsAsync(new List<ActionGroupState> { ActionGroupState.Started }, assignedAgvCorrelationId: assignedAgv.CorrelationId);
        bool otherActionGroupsStarted = startedActionGroups.Any(a => a.CorrelationId != action.ParentId);
        if (!otherActionGroupsStarted)
        {
            return true;
        }

        _logger.LogError("Could not start action {Action} of group {ActionGroup}, there are other started action groups for this agv.", action.CorrelationId, action.ParentId);
        await _actionGroupRepository.UpdateActionAndGroupStateAsync(action, action.Sequence == 0 ? assignedAgv : null, ActionState.Failed);
        await context.Publish(new StartingActionForAgvFailed(assignedAgv.CorrelationId));
        return false;
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

        await SelectNodesForAgvAction(actionGroup, action, assignedAgv, cancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="TryStartManualAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryStartManualAction> context)
    {
        var agvCorrelationId = context.Message.AgvCorrelationId;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        IReadOnlyList<Domain.Models.ActionGroup.ActionGroup> actionGroups = await _actionGroupRepository
            .GetActionGroupsAsync(ActionGroupState.Created, assignedAgvCorrelationId: agv.CorrelationId, isManual: true, maxItems: 1);

        var actionGroup = actionGroups.SingleOrDefault();
        var action = actionGroup?.FirstNotStartedAction;
        if (action == null)
        {
            _logger.LogDebug("There is no manual action available for agv with CorrelationId {AgvCorrelationId}.", agv.CorrelationId);
            return;
        }
        await SelectNodesForAgvAction(actionGroup!, action, agv, context.CancellationToken);
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
            return;
        }
        try
        {
            var action = await _actionGroupRepository.GetSubsequentActionAsync(agv.CorrelationId);
            _logger.LogDebug("Starting succeeding action {Action} of group {ActionGroup} for agv with CorrelationId: {AgvCorrelationId}.", action.CorrelationId, action.ParentId, agv.CorrelationId);
            var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(action.ParentId);
            await SelectNodesForAgvAction(actionGroup, action, agv, cancellationToken);
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

        var agvSettings = _settingsProvider.GetSettings();
        bool needsCharging = agv.CheckMustCharge(agvSettings);
        bool hasUnfinishedDropAction = await _actionGroupRepository.HasUnfinishedDropActionsAsync(agv.CorrelationId);

        bool canStartNewAction = !needsCharging && !agv.NeedsMaintenance && !hasUnfinishedDropAction && !agv.IsLocked;

        if (!canStartNewAction)
        {
            _logger.LogWarning("Can not start new action group for agv {Agv} (is locked: {IsLocked}/ has unfinished drop action: {HasUnfinishedDropAction}/ needs charging: {NeedsCharging}).", agv.CorrelationId, agv.IsLocked, hasUnfinishedDropAction, needsCharging);
            return;
        }

        await _actionManagerSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var agvData = await _agvDb.Data.GetAgvDataAsync(agv.CorrelationId);
            var actionGroup = await _chooseAgvActionGroupService.GetNotStartedActionGroupAsync(agv, agvData, cancellationToken);
            if (actionGroup == null)
            {
                _logger.LogDebug("There is no next action available for agv with CorrelationId {AgvCorrelationId}.", agv.CorrelationId);
                await context.Publish(new AgvCanStartIdleAction(agv.CorrelationId), cancellationToken);
                return;
            }
            var action = actionGroup.FirstNotStartedAction!;
            _logger.LogDebug("Trying to start action {ActionId} with type {ActionType}.", action.CorrelationId, Enum.GetName(action.ActionType));
            await SelectNodesForAgvAction(actionGroup, action, agv, context.CancellationToken);
            actionGroup.Assign(agv);
            await _actionGroupRepository.UpdateAsync(actionGroup);
        }, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="CreateParkingActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateParkingActionGroup> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        var agvSettings = _settingsProvider.GetSettings();
        if (agv.CheckShouldCharge(agvSettings) || agv.IsLocked)
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
        await PublishNodeStateChange(context, parkNode.Id);

        var groupCorrelationId = Guid.NewGuid();
        var actionGroup = new Domain.Models.ActionGroup.ActionGroup(
            groupCorrelationId,
            string.Empty,
            actions: new List<AgvAction> { new(Guid.NewGuid(), ActionType.Move, parkNode.Id, groupCorrelationId) },
            assignedAgv: agv.CorrelationId,
            creator: ActionGroupCreator.TransportOrder
        );

        await _actionGroupRepository.AddAsync(actionGroup);
        await context.Publish(new ActionGroupCreated(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId), cancellationToken);
        await context.Publish(new ActionGroupCreatedForAgv(_mapper.Map<ActionGroup>(actionGroup), agv.CorrelationId), cancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="StartChargingActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<StartChargingActionGroup> context)
    {
        (var agvCorrelationId, string? chargeStationId, var actionGroupCreator) = context.Message;
        bool chargerProvided = !string.IsNullOrEmpty(chargeStationId);

        var cancellationToken = context.CancellationToken;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        bool hasUnfinishedActionGroup = await _actionGroupRepository.HasUnfinishedActionGroupAsync(agv.CorrelationId);
        IReadOnlyList<Domain.Models.ActionGroup.ActionGroup> actionGroups = await _actionGroupRepository.GetActionGroupsAsync(new[] {ActionGroupState.Created}, assignedAgvCorrelationId: agvCorrelationId);
        _logger.LogDebug("Evaluating {Name}, has unfinished action: {HasUnfinishedActionGroup} for agv {CorrelationId}.", nameof(StartChargingActionGroup), hasUnfinishedActionGroup, agv.CorrelationId);

        if (agv.IsLocked || (hasUnfinishedActionGroup && !chargerProvided))
        {
            return;
        }

        if (agv.IsCharging || await _actionGroupRetrievalService.IsAgvHeadingToNodeOfType(agvCorrelationId, AgvGraph.NodeType.Charge))
        {
            _logger.LogDebug("No need for another charge action because the agv is already charging for agvId: {AgvId}.", agv.AgvId);
            // This might happen if the AssignAsync action is called several times due to a low SOC value, although the agv is already charging
            return;
        }

        // Yes, we want to charge
        _logger.LogInformation("Starting charge action for agv with CorrelationId: {AgvCorrelationId}.", agv.CorrelationId);

        // Lets see if there is a pending charge action group
        var actionGroup = actionGroups.FirstOrDefault(a => a.Actions.Any(ac => ac.ActionType == ActionType.Charge));

        if (actionGroup == null)
        {
            if (!chargerProvided)
            {
                var chargingNode = await _chargingPointManager.ReserveChargerAsync(agv);
                chargeStationId = chargingNode.Id;
            }
            await PublishNodeStateChange(context, chargeStationId!);

            var groupCorrelationId = Guid.NewGuid();
            actionGroup = new Domain.Models.ActionGroup.ActionGroup(
                groupCorrelationId,
                string.Empty,
                actions: new List<AgvAction>
                {
                    new(Guid.NewGuid(), ActionType.Move, chargeStationId!, groupCorrelationId)
                    {
                        Sequence = 0
                    },
                    new(Guid.NewGuid(), ActionType.Charge, chargeStationId!, groupCorrelationId)
                    {
                        Sequence = 1
                    }
                },
                assignedAgv: agv.CorrelationId,
                creator: actionGroupCreator,
                isManual: chargerProvided
            );

            await _actionGroupRepository.AddAsync(actionGroup);
            await context.Publish(new ActionGroupCreated(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId), cancellationToken);
        }
        if (!agv.IsLocked && !hasUnfinishedActionGroup && !agv.IsCharging)
        {
            await context.Publish(new ActionGroupCreatedForAgv(_mapper.Map<ActionGroup>(actionGroup), agv.CorrelationId), cancellationToken);
        }
    }

    /// <summary>
    /// Handles the <see cref="QueueActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<QueueActionGroup> context)
    {
        (var actionGroup, bool throwOnInvalid) = context.Message;
        var cancellationToken = context.CancellationToken;

        _logger.LogTrace("Validating ActionGroup with CorrelationId: {ActionGroupId}.", actionGroup.CorrelationId);

        var domainActionGroup = _mapper.Map<Domain.Models.ActionGroup.ActionGroup>(context.Message.ActionGroup);
        var validationResult = await _actionGroupValidationService.ValidateAsync(domainActionGroup);
        bool isValid = validationResult.IsValid;

        if (!isValid)
        {
            _logger.LogError("Validation result of ActionGroup with CorrelationId: {ActionGroupId} is not valid! Errors: {Errors}", actionGroup.CorrelationId, validationResult);
            if (throwOnInvalid)
            {
                throw new ValidationException(validationResult.Errors);
            }
            domainActionGroup.InvalidateActionGroup(validationResult.ToReasons());
        }
        Domain.Models.Agv.Agv? agv = null;

        if (isValid)
        {
            if (actionGroup.AssignedAgv != null)
            {
                agv = await _agvDb.Agvs.GetAgvAsync((Guid)actionGroup.AssignedAgv);
                await context.RespondAsync(new Success());
            }
            else
            {
                var agvSettings = _settingsProvider.GetSettings();
                await _actionManagerSemaphoreProvider.ExecuteLockedAsync(async () =>
                {
                    ICollection<Domain.Models.Agv.Agv> goodAgvs = (await _agvDb.Agvs.GetReadyAgvsAsync(minBatteryLevel: agvSettings.BatteryLevelLow)).ToList();
                    ICollection<AgvData> agvData = await _agvDb.Data.GetSelectedAgvDataAsync(goodAgvs.Select(a => a.CorrelationId).ToList());
                    agv = await _chooseAgvActionGroupService.GetAgvAsync(domainActionGroup, goodAgvs.ToList(), agvData.ToList(), cancellationToken);
                }, cancellationToken);

                if (agv != null)
                {
                    domainActionGroup.Assign(agv);
                }
            }
        }

        _logger.LogDebug("Adding ActionGroup with CorrelationId: {ActionGroupId}.", actionGroup.CorrelationId);
        await _actionGroupRepository.AddAsync(domainActionGroup);
        var actionGroupMessageContent = _mapper.Map<ActionGroup>(domainActionGroup);
        await _bus.Publish(new ActionGroupCreated(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);

        if (!isValid)
        {
            List<string> validationFailureReasons = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            _logger.LogDebug("ActionGroup with CorrelationId: {ActionGroupId} is not valid, publishing ActionGroupInvalid.", actionGroup.CorrelationId);
            await _bus.Publish(new ActionGroupInvalid(actionGroup.CorrelationId, actionGroupMessageContent, validationFailureReasons), cancellationToken);
            return;
        }

        if (agv != null)
        {
            var action = domainActionGroup.FirstNotStartedAction;
            await SelectNodesForAgvAction(domainActionGroup, action!, agv, cancellationToken);
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

        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByActionIdAsync(correlationId);
        var agvCorrelationId = actionGroup.AssignedAgv!.Value;
        actionGroup.CompleteAction(correlationId);
        await _actionGroupRepository.UpdateAsync(actionGroup);

        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);
        if (actionGroup.State == ActionGroupState.Completed)
        {
            await context.Publish(new ActionGroupCompleted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow), cancellationToken);
        }

        await context.Publish(new NonChargingActionCompleted(agvCorrelationId), cancellationToken);
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
            _logger.LogError("Could not abort action group {ActionGroup}, it is not assigned to an agv.", actionGroup.CorrelationId);
            await context.RespondAsync(new AbortActionGroupFailed("Action group abortion failed, no assigned agv."));
            return;
        }
        var action = actionGroup.Actions.FirstOrDefault(a => a.StartedDate != null && a.State != ActionState.Completed);
        if (action == null)
        {
            _logger.LogError("Could not abort action group {ActionGroup}, no started actions.", actionGroup.CorrelationId);
            await context.RespondAsync(new AbortActionGroupFailed("Action group abortion failed, no started actions."));
            return;
        }
        var agv = await _agvDb.Agvs.GetAgvAsync((Guid)actionGroup.AssignedAgv!);
        actionGroup = await _actionGroupRepository.UpdateActionAndGroupStateAsync(action, agv, ActionState.Aborted, context.Message.AbortReason.ToReason());

        await context.Publish(new ActionAborted(agv.CorrelationId, _mapper.Map<MessageAgvAction>(action)));
        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));
        await context.RespondAsync(new Success());

        var actionGroupWithNodes = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(actionGroup);
        if (actionGroupWithNodes.Actions.Any(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)))
        {
            string serviceNode = actionGroupWithNodes.Actions.First(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)).TargetNodeId;
            await context.Publish(new AutomaticActionWithdrawn(serviceNode));
        }
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

        actionGroup = await _actionGroupRepository.UpdateActionAndGroupStateAsync(actionGroup.FirstNotStartedAction!, null, ActionState.Canceled, context.Message.CancelReason.ToReason());
        await context.PublishAndRespond(new ActionGroupCanceled(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow));
        var actionGroupWithNodes = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(actionGroup);
        if (actionGroupWithNodes.Actions.Any(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)))
        {
            string serviceNode = actionGroupWithNodes.Actions.First(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)).TargetNodeId;
            await context.Publish(new AutomaticActionWithdrawn(serviceNode));
        }
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
        await _actionGroupRepository.UpdateAsync(actionGroup);
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

    private async Task SelectNodesForAgvAction(Domain.Models.ActionGroup.ActionGroup actionGroup, AgvAction action, Domain.Models.Agv.Agv assignedAgv, CancellationToken cancellationToken)
    {
        if (!assignedAgv.TryTransitionToBusy(ignoreLocked: true))
        {
            _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} is not ready to be set to busy.", assignedAgv.CorrelationId);
            return;
        }

        // here the state has changed in all cases
        await PublishStateChange(assignedAgv);

        var agvData = await _agvDb.Data.GetAgvDataAsync(assignedAgv.CorrelationId);
        var route = await _routeService.GetRouteAsync(assignedAgv, action.TargetNodeId, null, agvData, cancellationToken);
        action.Nodes = action.ActionType == ActionType.Charge ? new[] { action.TargetNodeId } : route.Nodes;

        _logger.LogDebug("Starting action with CorrelationId: {ActionCorrelationId} for Agv {AgvCorrelationId}. Setting Agv to busy in next step.", action.CorrelationId, assignedAgv.CorrelationId);
        try
        {
            if ((action.Nodes == null || !action.Nodes.Any()) && assignedAgv.AgvStatus!.LastNodeId != action.TargetNodeId)
            {
                throw new ArgumentNullException(nameof(action.Nodes));
            }

            // we only want to trigger and estimation when we start the first action of the group
            if (actionGroup.EstimatedDuration == default && actionGroup.Actions.Min(a => a.Sequence) == action.Sequence)
            {
                await _bus.Publish(new ActionGroupHasNoEstimatedDuration(Guid.NewGuid(), action, route.Costs, assignedAgv.CorrelationId, actionGroup.CorrelationId), cancellationToken);
            }

            await _bus.Publish(new NodesSelectedForAction(action, assignedAgv.CorrelationId), cancellationToken);
        }
        catch
        {
            _logger.LogWarning("Node selection failed for action: {Action}.", action);
        }
    }

    private async Task PublishStateChange(Domain.Models.Agv.Agv agv)
    {
        var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);
        await _bus.Publish(new AgvStateChanged(agv.CorrelationId, agv.CurrentAgvState, agv.PreviousAgvState, agvMessageContent));
    }

    private async Task PublishNodeStateChange(IPublishEndpoint context, string nodeId)
    {
        var nodeState = await _nodeStateRepository.GetNodeStateAsync(nodeId);
        if (nodeState == null)
        {
            _logger.LogError("Could not retrieve node state for node {NodeId}", nodeId);
            return;
        }
        await context.Publish(new NodeStateChanged(nodeState));
    }
}