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
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Constants;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Data.SqlClient;
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
    IConsumer<StartActionOfActionGroup>,
    IConsumer<TryStartManualAction>,
    IConsumer<TryStartNextActionInActionGroup>,
    IConsumer<TryStartNewActionGroup>,
    IConsumer<CreateParkingActionGroup>,
    IConsumer<StartChargingActionGroup>,
    IConsumer<CompleteActionOfActionGroup>,
    IConsumer<QueueActionGroup>,
    IConsumer<CancelActionGroup>,
    IConsumer<SelectNodesForAction>,
    IConsumer<AbortActionGroup>,
    IConsumer<ResetActionGroup>,
    IConsumer<EstimateActionGroupDuration>
{
    private readonly ILogger<ActionCommandHandler> _logger;
    private readonly IRouteService _routeService;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IChooseAgvActionGroupService _chooseAgvActionGroupService;
    private readonly ISemaphoreProvider<IAgvsSemaphore> _actionManagerSemaphoreProvider;
    private readonly ISettingsProvider<AgvSettings> _settingsProvider;
    private readonly IMapper _mapper;
    private readonly IBus _bus;
    private readonly IActionGroupValidationService _actionGroupValidationService;
    private readonly IActionGroupRetrievalService _actionGroupRetrievalService;
    private readonly IAgvDb _agvDb;
    private readonly IActionGroupDurationEstimationService _durationEstimationService;

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
        _actionGroupValidationService = actionGroupValidationService;
        _actionGroupRetrievalService = actionGroupRetrievalService;
        _durationEstimationService = durationEstimationService;
        _settingsProvider = settingsProvider;
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
        var assignedAgv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByActionIdAsync(action.CorrelationId);
        bool agvActionCanBeStarted = await CheckAgvActionCanBeStarted(context, action, assignedAgv, actionGroup);
        if (agvActionCanBeStarted)
        {
            actionGroup.StartAction(action.CorrelationId, action.Nodes);
            await TryUpdateActionGroup(actionGroup);
            var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
            await context.Publish(new StartActionCommand(assignedAgv.CorrelationId, assignedAgv.SerialNumber, _mapper.Map<MessageAgvAction>(action)), cancellationToken);
            await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);
            if (action.IsFirstAction)
            {
                _logger.LogOrderInformation(LogCodes.OrderStarted, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Order Started", $"Order {actionGroup.OrderId} has been started");
                await context.Publish(new ActionGroupStarted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow), cancellationToken);
            }
            _logger.LogOrderInformation(LogCodes.OrderActionStarted, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Order Updated", $"{action.ActionType} of order {actionGroup.OrderId} has been started");
        }
    }

    private async Task<bool> CheckAgvActionCanBeStarted(IPublishEndpoint context, AgvAction action, Domain.Models.Agv.Agv assignedAgv, Domain.Models.ActionGroup.ActionGroup actionGroup)
    {
        bool isNextActionInGroup = actionGroup.FirstNotStartedAction!.CorrelationId == action.CorrelationId;
        if (!isNextActionInGroup)
        {
            _logger.LogWarning("Preventing start of action {Action} of group {ActionGroupCorrelationId}, because action another should start first.", action.CorrelationId, actionGroup.CorrelationId);
            return false;
        }

        if (actionGroup.AssignedAgv.GetValueOrDefault() != assignedAgv.CorrelationId)
        {
            _logger.LogError("Could not start action {Action} of group {ActionGroupCorrelationId} for agv {AgvCorrelationId}, the action group is already handled by {OtherAgvCorrelationId}.", action.CorrelationId, action.ParentId, assignedAgv.CorrelationId, actionGroup.AssignedAgv);
            await context.Publish(new StartingActionForAgvFailed(assignedAgv.CorrelationId));
            return false;
        }

        IReadOnlyList<Domain.Models.ActionGroup.ActionGroup> startedActionGroups = await _actionGroupRepository.GetActionGroupsAsync(new List<ActionGroupState> {ActionGroupState.Started}, assignedAgvCorrelationId: assignedAgv.CorrelationId);
        bool otherActionGroupsStarted = startedActionGroups.Any(a => a.CorrelationId != action.ParentId);
        if (!otherActionGroupsStarted)
        {
            return true;
        }

        _logger.LogError("Could not start action {Action} of group {ActionGroupCorrelationId}, there are other started action groups for this agv.", action.CorrelationId, action.ParentId);
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

        if (await SelectNodesForAgvAction(action, assignedAgv, cancellationToken))
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

        var route = await _routeService.GetRouteAsync(assignedAgv, actionGroup.Actions.Select(a => a.TargetNodeId).ToList(), null, agvData);

        double duration = route.Costs;
        // costs for charging itself, just assume 1% / s, we don't have the charging rate here
        double chargeCostsPerCharge = 100 - assignedAgv.AgvStatus!.BatteryCharge;

        duration += actionGroup.Actions.Count(a => a.ActionType == ActionType.Charge) * chargeCostsPerCharge;

        // costs for action switching / state transitions of the Agv from one action to the next action
        duration += actionGroup.Actions.Count * ActionCosts.SwitchingActions;

        // average of unaccounted for costs (e.g. cargo loading and unloading) for pick/drop actions
        if (actionGroup.IsMovingCargo)
        {
            duration += await _durationEstimationService.GetAverageDurationEstimateDeviationAsync(actionGroup.AssignedAgv!.Value, actionGroup.GetTargets());
        }

        actionGroup.EstimatedDuration = TimeSpan.FromSeconds(duration);

        await TryUpdateActionGroup(actionGroup);
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

        IReadOnlyList<Domain.Models.ActionGroup.ActionGroup> actionGroups = await _actionGroupRepository
            .GetActionGroupsAsync(ActionGroupState.Created, assignedAgvCorrelationId: agv.CorrelationId, isManual: true, maxItems: 1);

        var actionGroup = actionGroups.SingleOrDefault();
        var action = actionGroup?.FirstNotStartedAction;
        if (action == null)
        {
            _logger.LogDebug("There is no manual action available for agv with CorrelationId {AgvCorrelationId}.", agv.CorrelationId);
            return;
        }
        if (await SelectNodesForAgvAction(action, agv, context.CancellationToken))
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
            return;
        }
        try
        {
            var action = await _actionGroupRepository.GetSubsequentActionAsync(agv.CorrelationId);
            _logger.LogDebug("Starting succeeding action {Action} of group {ActionGroupCorrelationId} for agv with CorrelationId: {AgvCorrelationId}.", action.CorrelationId, action.ParentId, agv.CorrelationId);
            if (!await SelectNodesForAgvAction(action, agv, cancellationToken))
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

        var agvSettings = _settingsProvider.GetSettings();
        bool needsCharging = agv.CheckMustCharge(agvSettings);
        bool hasUnfinishedDropAction = await _actionGroupRepository.HasUnfinishedDropActionsAsync(agv.CorrelationId);

        bool canStartNewAction = !needsCharging && !agv.NeedsMaintenance && !hasUnfinishedDropAction && !agv.IsLocked;

        if (!canStartNewAction)
        {
            _logger.LogWarning("Can not start new action group for agv {AgvCorrelationId} (is locked: {IsLocked}/ has unfinished drop action: {HasUnfinishedDropAction}/ needs charging: {NeedsCharging}).", agv.CorrelationId, agv.IsLocked, hasUnfinishedDropAction, needsCharging);
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
            _logger.LogDebug("Trying to start action {ActionId} of group {ActionGroupCorrelationId} with type {ActionType}.", action.CorrelationId, action.ParentId, Enum.GetName(action.ActionType));

            if (await SelectNodesForAgvAction(action, agv, context.CancellationToken))
            {
                var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(chosenActionGroup.CorrelationId);
                actionGroup.Assign(agv);
                _logger.LogOrderInformation(LogCodes.OrderAgvAssigned, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Agv was assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}");

                await TryUpdateActionGroup(actionGroup);
                await context.Publish(new NodesSelectedForAction(action, agv.CorrelationId), cancellationToken);
            }
        }, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="CreateParkingActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateParkingActionGroup> context)
    {
        var (groupCorrelationId, agvCorrelationId, parkNode) = context.Message;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        var actionGroup = new Domain.Models.ActionGroup.ActionGroup(
            groupCorrelationId,
            string.Empty,
            actions: new List<AgvAction> {new(Guid.NewGuid(), ActionType.Move, parkNode, groupCorrelationId)},
            assignedAgv: agv.CorrelationId,
            creator: ActionGroupCreator.TransportOrder
        );
        _logger.LogOrderInformation(LogCodes.OrderCreated, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Order created", $"Order {actionGroup.OrderId} was created successfully");
        _logger.LogOrderInformation(LogCodes.OrderAgvAssigned, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Agv was assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}");

        await _actionGroupRepository.AddAsync(actionGroup);
        await context.Publish(new ActionGroupCreated(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId), cancellationToken);
        await context.Publish(new ActionGroupCreatedForAgv(_mapper.Map<ActionGroup>(actionGroup), agv.CorrelationId), cancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="StartChargingActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<StartChargingActionGroup> context)
    {
        (var groupCorrelationId, var agvCorrelationId, string chargeStationId, var initiator) = context.Message;
        bool triggeredByWebApi = initiator == ActionGroupCreator.TransportOrderWebApi;

        var cancellationToken = context.CancellationToken;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        bool hasUnfinishedActionGroup = await _actionGroupRepository.HasUnfinishedActionGroupAsync(agv.CorrelationId);
        IReadOnlyList<Domain.Models.ActionGroup.ActionGroup> actionGroups = await _actionGroupRepository.GetActionGroupsAsync(new[] {ActionGroupState.Created}, assignedAgvCorrelationId: agvCorrelationId);
        _logger.LogDebug("Evaluating {Name}, has unfinished action: {HasUnfinishedActionGroup} for agv {AgvCorrelationId}.", nameof(StartChargingActionGroup), hasUnfinishedActionGroup, agv.CorrelationId);

        if (agv.IsLocked || (hasUnfinishedActionGroup && !triggeredByWebApi))
        {
            return;
        }

        if (agv.IsCharging || await _actionGroupRetrievalService.IsAgvHeadingToNodeOfType(agvCorrelationId, AgvGraph.NodeType.Charge))
        {
            _logger.LogDebug("No need for another charge action because the agv {SerialNumber}/{AgvCorrelationId} is already charging", agv.SerialNumber, agv.CorrelationId);
            // This might happen if the AssignAsync action is called several times due to a low SOC value, although the agv is already charging
            return;
        }

        // Yes, we want to charge
        _logger.LogInformation("Starting charge action for agv with CorrelationId: {AgvCorrelationId}.", agv.CorrelationId);

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
                isManual: triggeredByWebApi
            );

            await _actionGroupRepository.AddAsync(actionGroup);
            await context.Publish(new ActionGroupCreated(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId), cancellationToken);
            _logger.LogOrderInformation(LogCodes.OrderCreated, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Order created", $"Order {actionGroup.OrderId} was created successfully");
            _logger.LogOrderInformation(LogCodes.OrderAgvAssigned, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Agv was assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}");
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
            if (actionGroup.AssignedAgv != null)
            {
                agv = await _agvDb.Agvs.GetAgvAsync((Guid) actionGroup.AssignedAgv);
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

        if (agv != null)
        {
            _logger.LogOrderInformation(LogCodes.OrderAgvAssigned, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Agv was assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}");

            await context.Publish(new ActionGroupCreatedForAgv(actionGroupMessageContent, agv.CorrelationId), cancellationToken);
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
        actionGroup.CompleteAction(correlationId);
        await TryUpdateActionGroup(actionGroup);

        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);
        if (actionGroup.State == ActionGroupState.Completed)
        {
            await context.Publish(new ActionGroupCompleted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow), cancellationToken);
            _logger.LogOrderInformation(LogCodes.OrderCompleted, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Order Completed", $"Order {actionGroup.OrderId} was completed successfully");
        }
        else
        {
            _logger.LogOrderInformation(LogCodes.OrderActionCompleted, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Order Updated", $"{actionGroup.Actions.First(a => a.CorrelationId == correlationId).ActionType} of order {actionGroup.OrderId} has been completed");
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
            _logger.LogError("Could not abort action group {ActionGroupCorrelationId}, it is not assigned to an agv.", actionGroup.CorrelationId);
            await context.RespondAsync(new AbortActionGroupFailed("Action group abortion failed, no assigned agv."));
            return;
        }
        var action = actionGroup.Actions.FirstOrDefault(a => a.StartedDate != null && a.State != ActionState.Completed);
        if (action == null)
        {
            _logger.LogError("Could not abort action group {ActionGroupCorrelationId}, no started actions.", actionGroup.CorrelationId);
            await context.RespondAsync(new AbortActionGroupFailed("Action group abortion failed, no started actions."));
            return;
        }
        var agv = await _agvDb.Agvs.GetAgvAsync((Guid) actionGroup.AssignedAgv!);
        actionGroup = await _actionGroupRepository.UpdateActionAndGroupStateAsync(action, agv, ActionState.Aborted, context.Message.AbortReason.ToReason());

        await context.Publish(new ActionAborted(agv.CorrelationId, _mapper.Map<MessageAgvAction>(action)));
        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));
        await context.RespondAsync(new Success());
        _logger.LogOrderWarning(LogCodes.OrderAborted, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Order Aborted", $"Order {actionGroup.OrderId} was aborted due to {context.Message.AbortReason}");

        var actionGroupWithNodes = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(actionGroup);
        if (actionGroupWithNodes.Actions.Any(a => new[] {AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park}.Contains(a.TargetNodeType)))
        {
            string serviceNode = actionGroupWithNodes.Actions.First(a => new[] {AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park}.Contains(a.TargetNodeType)).TargetNodeId;
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
        _logger.LogOrderWarning(LogCodes.OrderCanceled, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Order Canceled", $"Order {actionGroup.OrderId} was canceled due to {context.Message.CancelReason}");

        var actionGroupWithNodes = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(actionGroup);
        if (actionGroupWithNodes.Actions.Any(a => new[] {AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park}.Contains(a.TargetNodeType)))
        {
            string serviceNode = actionGroupWithNodes.Actions.First(a => new[] {AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park}.Contains(a.TargetNodeType)).TargetNodeId;
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
        await TryUpdateActionGroup(actionGroup);
        var actionGroupWithNodes = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(actionGroup);
        if (actionGroupWithNodes.Actions.Any(a => new[] {AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park}.Contains(a.TargetNodeType)))
        {
            string serviceNode = actionGroupWithNodes.Actions.First(a => new[] {AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park}.Contains(a.TargetNodeType)).TargetNodeId;
            await context.Publish(new AutomaticActionWithdrawn(serviceNode));
        }
        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));
        await context.RespondAsync(new Success());
    }

    private async Task<bool> SelectNodesForAgvAction(AgvAction action, Domain.Models.Agv.Agv assignedAgv, CancellationToken cancellationToken)
    {
        if (!assignedAgv.TryTransitionToBusy(ignoreLocked: true))
        {
            _logger.LogDebug("Agv with CorrelationId {AgvCorrelationId} is not ready to be set to busy.", assignedAgv.CorrelationId);
            return false;
        }

        // here the state has changed in all cases
        await PublishStateChange(assignedAgv);

        var agvData = await _agvDb.Data.GetAgvDataAsync(assignedAgv.CorrelationId);
        var route = await _routeService.GetRouteAsync(assignedAgv, action.TargetNodeId, null, agvData, cancellationToken);
        action.Nodes = action.ActionType == ActionType.Charge ? [action.TargetNodeId] : route.Nodes;

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
        var agvMessageContent = _mapper.Map<Messages.MessageContent.Agv>(agv);
        await _bus.Publish(new AgvStateChanged(agv.CorrelationId, agv.CurrentAgvState, agv.PreviousAgvState, agvMessageContent));
    }

    private async Task TryUpdateActionGroup(Domain.Models.ActionGroup.ActionGroup actionGroup)
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
}