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
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.Acc.Common.Messages.ActionGroups.ActionGroup;
using ActionGroupCreated = Gebhardt.Acc.Common.Messages.ActionGroups.Events.ActionGroupCreated;
using Agv = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.Agv;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;
using AgvData = Gebhardt.Acc.Common.Domain.Models.AgvData;
using MessageAgvAction = Gebhardt.Acc.Common.Messages.ActionGroups.AgvAction;
using Route = Gebhardt.Acc.TransportOrder.Routing.Models.Route;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides handlers for <see cref="Action"/> related commands.
/// </summary>
[UsedImplicitly]
public class ActionCommandHandler :
    IConsumer<StartActionOfActionGroup>,
    IConsumer<TryStartNextActionInActionGroup>,
    IConsumer<TryStartNewActionGroup>,
    IConsumer<CreateParkingActionGroup>,
    IConsumer<CreateChargingActionGroup>,
    IConsumer<CreateQueueingActionGroup>,
    IConsumer<CompleteActionOfActionGroup>,
    IConsumer<QueueActionGroup>,
    IConsumer<CancelActionGroup>,
    IConsumer<AbortActionGroup>,
    IConsumer<ResetActionGroup>,
    IConsumer<SetTargetForAction>,
    IConsumer<EstimateActionGroupDuration>,
    IConsumer<PauseActionGroup>,
    IConsumer<ResumeActionGroup>,
    IConsumer<CancelOrAbortActionGroup>,
    IConsumer<SaveActionGroupAbortion>,
    IConsumer<TryUpdateIdleAction>,
    IConsumer<ReassignActionGroup>,
    IConsumer<CreateActionGroup>,
    IConsumer<ForceQueueActionGroup>
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
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IQueueManager _queueManager;

    private const string QueuingOrderId = "Queuing";

    /// <inheritdoc cref="ActionCommandHandler"/>
    public ActionCommandHandler(
        IRouteService routeService,
        IActionGroupRepository actionGroupRepository,
        IActionGroupValidationService actionGroupValidationService,
        IChooseAgvActionGroupService chooseAgvActionGroupService,
        IActionGroupRetrievalService actionGroupRetrievalService,
        ISemaphoreProvider<IAgvsSemaphore> actionManagerSemaphoreProvider,
        IActionGroupDurationEstimationService durationEstimationService,
        IQueueManager queueManager,
        IAgvDb agvDb,
        IMetricCollectionService metricCollectionService,
        IMapper mapper,
        IBus bus,
        ILogger<ActionCommandHandler> logger,
        ActionCommandHelper actionService,
        IPublishEndpoint publishEndpoint
    )
    {
        _actionService = actionService;
        _publishEndpoint = publishEndpoint;
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
        _queueManager = queueManager;
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
        bool agvActionCanBeStarted = await _actionService.CheckAgvActionCanBeStarted(context, action, assignedAgv, actionGroup);
        if (agvActionCanBeStarted)
        {
            bool? previousActionGroupDockFront = null;
            if (action.IsFirstAction)
            {
                previousActionGroupDockFront = await _actionGroupRepository.GetDockFrontOfLastCompletedAction(agvCorrelationId, actionGroup.CorrelationId);
            }
            string? targetNodeId = actionGroup.Actions.Single(a => a.CorrelationId == action.CorrelationId).Target.NodeId;

            var targetNode = targetNodeId != null ? await _agvDb.Graphs.GetNodeAsync(targetNodeId) : null;
            DockingDirection? nodeDockingDirection = targetNode?.DockingDirection;
            bool startNodeIsStation = await GetStartNodeIsStation(action);
            // ensure nodes are set before DockFront is diagnosed, to allow identification of Doppelspiel
            actionGroup.StartAction(action.CorrelationId, action.Nodes, action.Edges);
            actionGroup.SetDockFrontOfAction(action.CorrelationId, previousActionGroupDockFront, nodeDockingDirection, startNodeIsStation);
            actionGroup.SetCargoLiftHeightForAction(action.CorrelationId, targetNode?.PickLiftHeight, targetNode?.DropLiftHeight);
            await _actionService.TryUpdateActionGroup(actionGroup);
            action = actionGroup.Actions.Single(a => a.CorrelationId == action.CorrelationId);

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
                    $"Order {actionGroup.OrderId} has been started",
                    new Dictionary<string, object?>
                    {
                        { LogLabels.OrderId, actionGroup.OrderId }
                    });
                await context.Publish(new ActionGroupStarted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow), cancellationToken);
            }
            _logger.LogOrderInformation(
                LogCodes.OrderActionStarted,
                actionGroup.AssignedAgv.GetValueOrDefault(),
                actionGroup.CorrelationId,
                "Order Updated",
                $"{action.ActionType} of order {actionGroup.OrderId} has been started",
                new Dictionary<string, object?>
                {
                    { LogLabels.OrderId, actionGroup.OrderId },
                    { LogLabels.ActionType, action.ActionType }
                });
            _logger.LogInformation("{ActionType} for action group {CorrelationId} has started.", action.ActionType, actionGroup.CorrelationId);
            await _actionService.LogStationUpdate(assignedAgv, action, actionGroup);
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
        Task<Agv> agvTask = _agvDb.Agvs.GetAgvAsync(agvId);
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
        }

        if (lastActionWasMoveToCoordinate || action.IsMoveToCoordinate)
        {
            duration += await _durationEstimationService.GetDurationEstimate(agvData!.MaxVelocityStraight, actionGroup.Actions.First(), assignedAgv.CorrelationId);
        }

        duration += await _durationEstimationService.AddNonRouteCosts(lastActionWasMoveToCoordinate, actionGroup, action, assignedAgv);

        actionGroup.EstimatedDuration = TimeSpan.FromSeconds(duration);

        await _actionService.TryUpdateActionGroup(actionGroup);
        await context.Publish(new ActionDurationEstimated(action, agvId, actionGroupId));
        _logger.LogDebug("Estimated duration for action group {ActionGroupCorrelationId} and agv {AgvCorrelationId}: {EstimatedDuration}", actionGroup.CorrelationId, assignedAgv.CorrelationId, actionGroup.EstimatedDuration);
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
            _logger.LogWarning("No succeeding action for agv with CorrelationId: {AgvCorrelationId}.", agv.CorrelationId);
            return;
        }

        if (agv is { CanNotStartNewAction: true, HasError: false })
        {
            _logger.LogWarning("Can not start next action in group: Agv is pending mode switch with current action group policy {Policy}", agv.Mode.CurrentActionGroupPolicy);
            return;
        }

        try
        {
            var action = await _actionGroupRepository.GetSubsequentActionAsync(agv.CorrelationId);

            var actionGroupId = action.ParentId;

            var actionGroup = await _actionGroupRepository.GetActionGroupAsync(actionGroupId);

            if (agv.HasError && !(actionGroup?.IsManual ?? false))
            {
                _logger.LogWarning("Cannot start automatic action {Action} of group {ActionGroup} while agv {Agv} has an error", action.CorrelationId, actionGroupId, agv.CorrelationId);
                return;
            }

            if (action.ShouldSkip)
            {
                if (actionGroup == null)
                {
                    throw new ActionGroupNotFoundException(actionGroupId);
                }
                action = await ActionCommandHelper.HandleSkippedActions(context, action, agv.CorrelationId, actionGroup, cancellationToken);
            }

            if (action == null)
            {
                _logger.LogDebug("Not starting new action for {ActionGroupCorrelationId} for agv with CorrelationId: {AgvCorrelationId} because all remaining actions were marked to be skipped.", actionGroupId, agv.CorrelationId);
                return;
            }

            _logger.LogDebug("Starting succeeding action {Action} of group {ActionGroupCorrelationId} for agv with CorrelationId: {AgvCorrelationId}.", action.CorrelationId, actionGroupId, agv.CorrelationId);
            if (action.IsIncomplete)
            {
                _logger.LogDebug("Succeeding action {Action} of group {ActionGroupCorrelationId} is incomplete, thus not started for agv with CorrelationId: {AgvCorrelationId}.", action.CorrelationId, actionGroupId, agv.CorrelationId);
                await context.Publish(new StartingActionForAgvFailed(agvCorrelationId), cancellationToken);
                return;
            }

            if (action.State == ActionState.Paused)
            {
                _logger.LogDebug("Succeeding action {Action} of group {ActionGroupCorrelationId} is paused, thus not started for agv with CorrelationId: {AgvCorrelationId}.", action.CorrelationId, actionGroupId, agv.CorrelationId);
                return;
            }

            if (!await _actionService.SelectNodesForAgvAction(action, agv, true, agv.HasError, cancellationToken))
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

        if (agv.IsLocked)
        {
            _logger.LogWarning("Can not start new action group for agv, agv {AgvCorrelationId} is locked: {IsLocked}", agv.CorrelationId, agv.IsLocked);
            return;
        }

        if (agv.ShouldStartManualAction || agv.HasError)
        {
            var manualAction = await _actionService.GetManualAction(agv);
            if (manualAction == null)
            {
                _logger.LogDebug("There is no un-started manual action available for agv with CorrelationId {AgvCorrelationId}.", agv.CorrelationId);
                return;
            }
            if (await _actionService.SelectNodesForAgvAction(manualAction, agv, false, true, context.CancellationToken))
            {
                await context.Publish(new NodesSelectedForAction(manualAction, agv.CorrelationId), context.CancellationToken);
            }
            return;
        }

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
                _logger.LogDebug("There is no next action available for agv with CorrelationId {AgvCorrelationId}", agv.CorrelationId);
                await context.Publish(new AgvCanStartIdleAction(agv.CorrelationId), cancellationToken);
                return;
            }

            var action = chosenActionGroup.FirstNotStartedAction!;
            _logger.LogDebug("Trying to start action {ActionId} of group {ActionGroupCorrelationId} with type {ActionType}", action.CorrelationId, action.ParentId, Enum.GetName(action.ActionType));

            if (await _actionService.SelectNodesForAgvAction(action, agv, false, false, context.CancellationToken))
            {
                var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(chosenActionGroup.CorrelationId);
                if (actionGroup.AssignedAgv == null)
                {
                    actionGroup.Assign(agv);
                    _logger.LogOrderInformation(LogCodes.OrderAgvAssigned, agv.CorrelationId, actionGroup.CorrelationId,
                        "AGV Assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}",
                        new Dictionary<string, object?>
                        {
                            { LogLabels.OrderId, actionGroup.OrderId }, { LogLabels.AgvSerialNumber, agv.SerialNumber }
                        });
                }
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
            hasPriority: false,
            isQueued: true
        );

        bool agvModeProhibitsCreation = agv.IsLocked || !agv.CanStartAutomaticAction || agv.HasPendingModeSwitch;

        if (agvModeProhibitsCreation)
        {
            _logger.LogInformation("Could not create parking action group for AGV:{CorrelationId}. locked:{IsLocked}, CanStartAutomaticAction:{CanStartAutomaticAction}, pending mode :{PendingMode}",
                agv.CorrelationId,
                agv.IsLocked,
                agv.CanStartAutomaticAction, agv.Mode.PendingType);
            return;
        }
        _logger.LogOrderInformation(LogCodes.OrderCreated, agv.CorrelationId, actionGroup.CorrelationId,
            "Order Created", $"Order {actionGroup.OrderId} was created successfully",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId } });
        _logger.LogOrderInformation(LogCodes.OrderAgvAssigned, agv.CorrelationId, actionGroup.CorrelationId,
            "AGV Assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId }, { LogLabels.AgvSerialNumber, agv.SerialNumber } });

        if (!await _actionGroupRepository.AddAsync(actionGroup))
        {
            _logger.LogError("Failed to save action group {ActionGroupCorrelationId}", actionGroup.CorrelationId);
            throw new ActionsInActionGroupEmptyException(actionGroup.CorrelationId);
        }
        await context.Publish(new ActionGroupCreated(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId, agv.CorrelationId), cancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="CreateQueueingActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateQueueingActionGroup> context)
    {
        (var groupCorrelationId, var agvCorrelationId, string queueNode) = context.Message;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        var actionGroup = new Domain.Models.ActionGroup.ActionGroup(
            groupCorrelationId,
            QueuingOrderId,
            actions: new List<AgvAction> { new(Guid.NewGuid(), ActionType.Move, queueNode, groupCorrelationId) },
            assignedAgv: agv.CorrelationId,
            creator: ActionGroupCreator.TransportOrder,
            hasPriority: true,
            isQueued: true
        );

        bool agvModeProhibitsCreation = agv.IsLocked || !agv.CanStartAutomaticAction || agv.HasPendingModeSwitch;

        if (agvModeProhibitsCreation)
        {
            _logger.LogInformation("Could not create queueing action group for AGV:{CorrelationId}. locked:{IsLocked}, CanStartAutomaticAction:{CanStartAutomaticAction}, pending mode :{PendingMode}",
                agv.CorrelationId,
                agv.IsLocked,
                agv.CanStartAutomaticAction, agv.Mode.PendingType);
            return;
        }
        _logger.LogOrderInformation(LogCodes.OrderCreated, agv.CorrelationId, actionGroup.CorrelationId,
            "Order Created", $"Order {actionGroup.OrderId} was created successfully",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId } });
        _logger.LogOrderInformation(LogCodes.OrderAgvAssigned, agv.CorrelationId, actionGroup.CorrelationId,
            "AGV Assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId }, { LogLabels.AgvSerialNumber, agv.SerialNumber } });

        if (!await _actionGroupRepository.AddAsync(actionGroup))
        {
            _logger.LogError("Failed to save action group {ActionGroupCorrelationId}", actionGroup.CorrelationId);
            throw new ActionsInActionGroupEmptyException(actionGroup.CorrelationId);
        }
        await context.Publish(new ActionGroupCreated(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId, agv.CorrelationId), cancellationToken);
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

        bool agvModeProhibitsCreation = agv.IsLocked || ((!agv.CanStartAutomaticAction || agv.HasPendingModeSwitch) && !triggeredByWebApi);

        if (agvModeProhibitsCreation)
        {
            _logger.LogInformation("Could not create charging action group for AGV:{CorrelationId}. locked:{IsLocked}, CanStartAutomaticAction:{CanStartAutomaticAction}, TriggeredByWebApi:{TriggeredByWebApi}, pending mode :{PendingMode}",
                agv.CorrelationId,
                agv.IsLocked,
                agv.CanStartAutomaticAction,
                triggeredByWebApi, agv.Mode.PendingType);
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
            _logger.LogInformation("AGV:{SerialNumber}/{AgvCorrelationId} is already heading to a charging station", agv.SerialNumber, agv.CorrelationId);
            // This might happen if the AssignAsync action is called several times due to a low SOC value, although the agv is already charging
            return;
        }

        IReadOnlyList<Domain.Models.ActionGroup.ActionGroup> actionGroups = await _actionGroupRepository.GetCreatedAndStartedActionGroupsForAgvAsync(agvCorrelationId);

        var agvSettings = _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
        bool agvMustCharge = agv.CheckMustCharge(agvSettings);
        bool hasNotStartedActionGroups = actionGroups.Any();

        var startedChargingAction = actionGroups
            .FirstOrDefault(ag => ag.State == ActionGroupState.Started && ag.Actions.Any(a => a.ActionType == ActionType.Charge));
        if (startedChargingAction != null)
        {
            _logger.LogInformation("AGV:{CorrelationID} has already started an action group for charging", agv.CorrelationId);
            return;
        }

        if (!agvMustCharge && hasNotStartedActionGroups && initiator != ActionGroupCreator.TransportOrderWebApi)
        {
            _logger.LogInformation("Skip charging for AGV:{CorrelationID}. Must charge: {AgvMustCharge}. Action groups not started: {HasNotStartedActionGroups}. Initiator: {Initiator}",
                agv.CorrelationId,
                agvMustCharge,
                hasNotStartedActionGroups,
                initiator);
            return;
        }

        // Yes, we want to charge
        _logger.LogInformation("Creating charge action for agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);

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
                        Sequence = 0,
                        InfoToAgv = new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.DockFront, true.ToString() } }
                    },
                    new(Guid.NewGuid(), ActionType.Charge, chargeStationId, groupCorrelationId)
                    {
                        Sequence = 1,
                        InfoToAgv = new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.DockFront, true.ToString() } }
                    }
                },
                assignedAgv: agv.CorrelationId,
                creator: initiator,
                isManual: triggeredByWebApi,
                hasPriority: triggeredByWebApi,
                isQueued: true
            );

            if (!await _actionGroupRepository.AddAsync(actionGroup))
            {
                _logger.LogError("Failed to save action group {ActionGroupCorrelationId}", actionGroup.CorrelationId);
                throw new ActionsInActionGroupEmptyException(actionGroup.CorrelationId);
            }
            await _publishEndpoint.Publish(new ActionGroupCreated(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId, agv.CorrelationId), cancellationToken);
            _logger.LogOrderInformation(LogCodes.OrderCreated, agv.CorrelationId, actionGroup.CorrelationId,
                "Order Created", $"Order {actionGroup.OrderId} was created successfully",
                new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId } });
            _logger.LogOrderInformation(LogCodes.OrderAgvAssigned, agv.CorrelationId, actionGroup.CorrelationId,
                "AGV Assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}",
                new Dictionary<string, object?>
                    { { LogLabels.OrderId, actionGroup.OrderId }, { LogLabels.AgvSerialNumber, agv.SerialNumber } });
        }
    }

    private async Task<Domain.Models.ActionGroup.ActionGroup> ValidDateActionGroup(Domain.Models.ActionGroup.ActionGroup actionGroup, bool throwOnInvalid)
    {
        _logger.LogTrace("Validating ActionGroup with CorrelationId: {ActionGroupCorrelationId}.", actionGroup.CorrelationId);
        var validationResult = await _actionGroupValidationService.ValidateAsync(actionGroup);
        bool isValid = validationResult.IsValid;

        if (!isValid)
        {
            _logger.LogError("Validation result of ActionGroup with CorrelationId: {ActionGroupCorrelationId} is not valid! Errors: {Errors}", actionGroup.CorrelationId, validationResult);
            if (throwOnInvalid)
            {
                throw new ValidationException(validationResult.Errors);
            }
            actionGroup.InvalidateActionGroup(validationResult.ToReasons());
            var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
            List<string> validationFailureReasons = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            _logger.LogOrderWarning(LogCodes.OrderInvalid, actionGroupMessageContent.AssignedAgv.GetValueOrDefault(),
                actionGroupMessageContent.CorrelationId, "Order Invalid",
                $"Order {actionGroupMessageContent.OrderId} is invalid due to {string.Join(",", validationFailureReasons)}",
                new Dictionary<string, object?>
                {
                    { LogLabels.OrderId, actionGroupMessageContent.OrderId },
                    { LogLabels.Reason, string.Join(",", validationFailureReasons) },
                    { LogLabels.ReasonDescription, string.Join(",", validationFailureReasons) }
                });

            await _bus.Publish(new ActionGroupInvalid(actionGroupMessageContent.CorrelationId, actionGroupMessageContent, validationFailureReasons));
        }

        return actionGroup;
    }

    /// <summary>
    /// Handles the <see cref="QueueActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<QueueActionGroup> context)
    {
        (var actionGroupMessageContent, bool throwOnInvalid) = context.Message;
        var cancellationToken = context.CancellationToken;

        var actionGroup = _mapper.Map<Domain.Models.ActionGroup.ActionGroup>(actionGroupMessageContent);

        actionGroup = await ValidDateActionGroup(actionGroup, throwOnInvalid);
        if (actionGroup.State != ActionGroupState.Invalid)
        {
            actionGroup.Queue();
        }

        bool updated = await _actionGroupRepository.AddOrUpdateAsync(actionGroup);
        actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        if (!updated)
        {
            _logger.LogOrderInformation(LogCodes.OrderCreated, actionGroupMessageContent.AssignedAgv.GetValueOrDefault(),
                actionGroupMessageContent.CorrelationId, "Order Created", $"Order {actionGroupMessageContent.OrderId} was created successfully",
                new Dictionary<string, object?> { { LogLabels.OrderId, actionGroupMessageContent.OrderId } });

            await _bus.Publish(new ActionGroupCreated(actionGroupMessageContent, actionGroupMessageContent.CorrelationId, actionGroupMessageContent.AssignedAgv), cancellationToken);
        }

        if (actionGroup.IsQueued)
        {
            _logger.LogOrderInformation(LogCodes.OrderQueued, actionGroupMessageContent.AssignedAgv.GetValueOrDefault(),
                actionGroupMessageContent.CorrelationId, "Order Queued", $"Order {actionGroupMessageContent.OrderId} has been queued",
                new Dictionary<string, object?> { { LogLabels.OrderId, actionGroupMessageContent.OrderId } });
            await _bus.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);
        }

        if (actionGroupMessageContent.AssignedAgv != null && actionGroup.IsQueued)
        {
            var agv = await _agvDb.Agvs.GetAgvAsync(actionGroupMessageContent.AssignedAgv.Value);
            _logger.LogOrderInformation(LogCodes.OrderAgvAssigned, actionGroupMessageContent.AssignedAgv.Value,
                actionGroup.CorrelationId, "AGV Assigned",
                $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}",
                new Dictionary<string, object?>
                    { { LogLabels.OrderId, actionGroup.OrderId }, { LogLabels.AgvSerialNumber, agv.SerialNumber } });
            await context.RespondAsync(new Success());
        }
    }

    /// <summary>
    /// Handles the <see cref="CreateActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateActionGroup> context)
    {
        var actionGroupMessageContent = context.Message.ActionGroup;
        var cancellationToken = context.CancellationToken;

        var actionGroup = _mapper.Map<Domain.Models.ActionGroup.ActionGroup>(actionGroupMessageContent);

        actionGroup = await ValidDateActionGroup(actionGroup, false);

        bool updated = await _actionGroupRepository.AddOrUpdateAsync(actionGroup);
        actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        if (!updated)
        {
            _logger.LogOrderInformation(LogCodes.OrderCreated, actionGroupMessageContent.AssignedAgv.GetValueOrDefault(),
                actionGroupMessageContent.CorrelationId, "Order Created", $"Order {actionGroupMessageContent.OrderId} was created successfully",
                new Dictionary<string, object?> { { LogLabels.OrderId, actionGroupMessageContent.OrderId } });

            await _bus.Publish(new ActionGroupCreated(actionGroupMessageContent, actionGroupMessageContent.CorrelationId, actionGroupMessageContent.AssignedAgv), cancellationToken);
        }

        if (actionGroupMessageContent.AssignedAgv != null)
        {
            var agv = await _agvDb.Agvs.GetAgvAsync(actionGroupMessageContent.AssignedAgv.Value);
            _logger.LogOrderInformation(LogCodes.OrderAgvAssigned, actionGroupMessageContent.AssignedAgv.Value,
                actionGroup.CorrelationId, "AGV Assigned",
                $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}",
                new Dictionary<string, object?>
                    { { LogLabels.OrderId, actionGroup.OrderId }, { LogLabels.AgvSerialNumber, agv.SerialNumber } });
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
        if (action.ActionType is ActionType.Pick or ActionType.Drop && context.Message.Creator == ActionGroupCreator.TransportOrder)
        {
            // Avoid completion of a pick or drop message due to 'completed' event race conditions during order update,
            // i.e., the agv sending a completed event of a move (to charger or node) that has already been updated to a pick or drop
            bool hasCargo = await _agvDb.Cargos.HasAgvCargoAsync(agvCorrelationId);
            if ((hasCargo && action.ActionType == ActionType.Drop) || (!hasCargo && action.ActionType == ActionType.Pick))
            {
                _logger.LogWarning("Agv {AgvCorrelationId} has cargo {HasCargo}, not completing action {ActionCorrelationId} of type {ActionType} yet", agvCorrelationId, hasCargo, action.CorrelationId, action.ActionType);
                throw new CargoStateMismatchException(action.CorrelationId, hasCargo);
            }
        }
        actionGroup.CompleteAction(correlationId);
        await _actionService.TryUpdateActionGroup(actionGroup);

        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);

        if (!agv.CanStartAutomaticAction && !actionGroup.IsManual && actionGroup.State != ActionGroupState.Paused)
        {
            var pauseReason = agv.Mode.GetPauseReason();
            _logger.LogInformation("CompleteActionOfActionGroup: ActionGroupId: {ActionGroupId} with reason: {Reason}", actionGroup.CorrelationId, pauseReason);
            await context.Publish(new PauseActionGroup(actionGroup.CorrelationId, pauseReason), cancellationToken);
        }

        if (action.ActionType == ActionType.Pick)
        {
            await context.Publish(new PickActionCompleted(actionGroup.CorrelationId, action.CorrelationId, action.CompletedDate.GetValueOrDefault()), cancellationToken);
        }

        if (action.ActionType == ActionType.Drop)
        {
            await _actionService.TryQueueDoppelspielPartner(context, actionGroup, action, agv, cancellationToken);
            await context.Publish(new DropActionCompleted(actionGroup.CorrelationId, action.CorrelationId, action.CompletedDate.GetValueOrDefault()), cancellationToken);
        }

        if (actionGroup.State != ActionGroupState.Completed)
        {
            await context.Publish(new NonChargingActionCompleted(agvCorrelationId), cancellationToken);
            _logger.LogOrderInformation(LogCodes.OrderActionCompleted, actionGroup.AssignedAgv.GetValueOrDefault(),
                actionGroup.CorrelationId, "Order Updated",
                $"{actionGroup.Actions.First(a => a.CorrelationId == correlationId).ActionType.ToString()} of order {actionGroup.OrderId} has been completed",
                new Dictionary<string, object?>
                {
                    { LogLabels.ActionType, actionGroup.Actions.First(a => a.CorrelationId == correlationId).ActionType.ToString() },
                    { LogLabels.OrderId, actionGroup.OrderId }
                });
            if (agv is { HasPendingModeSwitch: true, Mode.CurrentActionGroupPolicy: CurrentActionGroupPolicy.FinishAction })
            {
                await context.Publish(new SwitchMode(agv.CorrelationId), cancellationToken);
            }
            return;
        }

        bool isQueueing = agv.IsQueueing;
        if (agv.IsQueueing && action.Target.NodeId != null)
        {
            bool isAgvAtQueueHead = await _queueManager.TryReachQueueNode(agv, action.Target.NodeId);
            isQueueing = !isAgvAtQueueHead;
            await context.Publish(new TryEnqueueAgv(agv.CorrelationId, action.Target.NodeId), cancellationToken);
        }

        if (agv.HasPendingModeSwitch && agv.Mode.CurrentActionGroupPolicy != CurrentActionGroupPolicy.Pause)
        {
            await context.Publish(new SwitchMode(agv.CorrelationId), cancellationToken);
        }
        await context.Publish(new NonChargingActionCompleted(agvCorrelationId, isQueueing), cancellationToken);
        await context.Publish(new ActionGroupCompleted(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow), cancellationToken);
        _logger.LogOrderInformation(LogCodes.OrderCompleted, actionGroup.AssignedAgv.GetValueOrDefault(),
            actionGroup.CorrelationId, "Order Completed", $"Order {actionGroup.OrderId} was completed successfully",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId } });
        await _metricCollectionService.CollectOrderCompletionMetrics(actionGroup);
    }

    /// <summary>
    /// Handles the <see cref="AbortActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AbortActionGroup> context)
    {
        var (correlationId, abortReason) = context.Message;

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
        actionGroup = await _actionGroupRepository.UpdateActionAndGroupStateAsync(action, agv, ActionState.Aborted, abortReason.ToReason());

        if (actionGroup is { IsQueueingOrder: true, OrderId: QueuingOrderId } && agv.IsQueueing)
        {
            if (await _queueManager.RemoveAgvFromQueueEntry(agv.CorrelationId, action.Target.NodeId!))
            {
                await context.Publish(new LeaveQueue(agv.CorrelationId));
            }
        }

        await context.Publish(new ActionAborted(agv.CorrelationId, _mapper.Map<MessageAgvAction>(action)));
        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));
        await context.RespondAsync(new Success());
        _logger.LogOrderWarning(LogCodes.OrderAborted, actionGroup.AssignedAgv.GetValueOrDefault(),
            actionGroup.CorrelationId, "Order Aborted",
            $"Order {actionGroup.OrderId} was aborted due to {context.Message.AbortReason}",
            new Dictionary<string, object?>
            {
                { LogLabels.OrderId, actionGroup.OrderId },
                { LogLabels.Reason, context.Message.AbortReason },
                { LogLabels.ReasonDescription, context.Message.AbortReason }
            });

        var actionGroupWithNodes = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(actionGroup);
        if (actionGroupWithNodes.Actions.Any(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)))
        {
            string serviceNode = actionGroupWithNodes.Actions.First(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)).TargetNodeId;
            await context.Publish(new AutomaticActionWithdrawn(actionGroup.AssignedAgv, serviceNode, true));
        }
        await _metricCollectionService.CollectOrderCompletionMetrics(actionGroup, agv.AgvStatus!.LastNodeId);
    }

    /// <summary>
    /// Handles the <see cref="CancelActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CancelActionGroup> context)
    {
        var (actionGroupId, cancelReason) = context.Message;
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

        actionGroup = await _actionGroupRepository.UpdateActionAndGroupStateAsync(actionGroup.FirstNotStartedAction ?? actionGroup.Actions.First(), null, ActionState.Canceled, cancelReason.ToReason());
        await context.PublishAndRespond(new ActionGroupCanceled(actionGroupMessageContent, actionGroup.CorrelationId, DateTime.UtcNow));
        _logger.LogOrderWarning(LogCodes.OrderCanceled, actionGroup.AssignedAgv.GetValueOrDefault(),
            actionGroup.CorrelationId, "Order Canceled",
            $"Order {actionGroup.OrderId} was canceled due to {context.Message.CancelReason}",
            new Dictionary<string, object?>
            {
                { LogLabels.OrderId, actionGroup.OrderId },
                { LogLabels.Reason, context.Message.CancelReason },
                { LogLabels.ReasonDescription, context.Message.CancelReason }
            });

        var actionGroupWithNodes = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(actionGroup);
        if (actionGroupWithNodes.Actions.Any(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)))
        {
            string serviceNode = actionGroupWithNodes.Actions.First(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)).TargetNodeId;
            await context.Publish(new AutomaticActionWithdrawn(actionGroup.AssignedAgv, serviceNode, true));
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

        actionGroup.ResetActionGroup(!actionGroup.IsManual);
        await _actionService.TryUpdateActionGroup(actionGroup);
        var actionGroupWithNodes = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(actionGroup);
        if (actionGroupWithNodes.Actions.Any(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)))
        {
            string serviceNode = actionGroupWithNodes.Actions.First(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)).TargetNodeId;
            await context.Publish(new AutomaticActionWithdrawn(actionGroup.AssignedAgv, serviceNode, true));
        }
        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));
        _logger.LogOrderWarning(LogCodes.OrderReset, actionGroup.AssignedAgv.GetValueOrDefault(),
            actionGroup.CorrelationId, "Order Reset", $"Order {actionGroup.OrderId} was reset ",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId } });

        await context.Publish(new ActionGroupReassignedOrReset(actionGroup.CorrelationId, actionGroup.Creator));

        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="PauseActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<PauseActionGroup> context)
    {
        var (actionGroupId, pauseReason) = context.Message;

        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupId);

        _logger.LogInformation("Action group {ActionGroupId} has state {ActionGroupState} and reason {Reason} for pause.", actionGroupId, actionGroup.State, string.Join(", ", actionGroup.Reasons.Select(r => r.Template)));

        if (actionGroup.State is not (ActionGroupState.Started or ActionGroupState.Paused))
        {
            _logger.LogWarning("Can not pause action group {ActionGroupId} because it is in state {ActionGroupState}.", actionGroupId, actionGroup.State);
            return;
        }
        var agv = await _agvDb.Agvs.GetAgvAsync(actionGroup.AssignedAgv.GetValueOrDefault());
        var activeAction = actionGroup.FirstNotCompletedAction;
        ActionState? actionState = activeAction?.State;
        actionGroup.PauseActionGroup(pauseReason.ToReason());

        await _actionService.TryUpdateActionGroup(actionGroup);

        _logger.LogInformation("PauseActionGroup: Action state is {ActionState} and active action is {Action}", actionState, activeAction);
        if (actionState == ActionState.Started)
        {
            if (activeAction != null)
            {
                var actionMessage = _mapper.Map<MessageAgvAction>(activeAction);
                await context.Publish(new ActionAborted(agv.CorrelationId, actionMessage, true));
            }

            if (agv is { HasPendingModeSwitch: true, Mode.CurrentActionGroupPolicy: CurrentActionGroupPolicy.Pause })
            {
                await context.Publish(new SwitchMode(agv.CorrelationId));
            }

            if (activeAction != null)
            {
                var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
                await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));
            }
        }

        _logger.LogOrderInformation(LogCodes.OrderPaused, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroupId,
            "Order Paused", $"Order {actionGroup.OrderId} has been paused",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId }, { LogLabels.Reason, pauseReason } });
    }

    /// <summary>
    /// Handles the <see cref="ResumeActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResumeActionGroup> context)
    {
        (var actionGroupId, string? _, bool removeStationReason) = context.Message;

        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupId);

        if (actionGroup.State != ActionGroupState.Paused)
        {
            _logger.LogWarning("Can not resume action group {ActionGroupId} because it is in state {ActionGroupState}.", actionGroupId, actionGroup.State);
            return;
        }

        actionGroup.ResumeActionGroup(removeStationReason);

        await _actionService.TryUpdateActionGroup(actionGroup);

        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));

        if (actionGroup.State != ActionGroupState.Paused)
        {
            _logger.LogOrderInformation(LogCodes.OrderResumed, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroupId,
                "Order Resumed", $"Order {actionGroup.OrderId} has been resumed",
                new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId } });

            await context.Publish(new ActionGroupResumed(actionGroup.CorrelationId, actionGroup.AssignedAgv!.Value, context.Message.InitiatingUser));
        }
    }

    /// <summary>
    /// Handles the <see cref="SetTargetForAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SetTargetForAction> context)
    {
        (var actionId, var actionGroupId, Guid? assignedAgv, string targetNodeId) = context.Message;

        var cancellationToken = context.CancellationToken;

        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupId);

        var nextAction = actionGroup.Actions.First(a => a.CorrelationId == actionId);

        var currentAction = actionGroup.GetPreviousAction(actionId);

        bool stationIsLocked = await _actionService.StationIsLockedIfExists(targetNodeId);
        bool shouldUpdate = !stationIsLocked && currentAction is { State: ActionState.Started } && assignedAgv != null;
        if (nextAction.State == ActionState.Started || !nextAction.IsIncomplete)
        {
            // Reset of previously set action target: no next action needs to be skipped, i.e. next action is current action
            currentAction = nextAction;
            shouldUpdate = !stationIsLocked && assignedAgv != null;
        }

        Agv? agv = null;
        Route? route = null;
        if (shouldUpdate)
        {
            agv = await _agvDb.Agvs.GetAgvAsync(assignedAgv!.Value);
            var agvData = await _agvDb.Data.GetAgvDataAsync(assignedAgv.Value);
            route = await _routeService.GetRouteAsync(agv, targetNodeId, null, agvData, cancellationToken);
            if (currentAction?.CorrelationId == nextAction.CorrelationId)
            {
                actionGroup.SetTargetForAction(actionId, targetNodeId, route);
            }
            else
            {
                currentAction = actionGroup.SetTargetForActionWithUpdate(actionId, targetNodeId, route);
            }
            bool startNodeIsStation = await GetStartNodeIsStation(currentAction);
            bool? previousActionGroupDockFront = null;
            if (currentAction.IsFirstAction)
            {
                // For diagnosing DockFront of the first action of the group we need the docking direction of the last completed action
                previousActionGroupDockFront = await _actionGroupRepository.GetDockFrontOfLastCompletedAction(agv.CorrelationId, actionGroup.CorrelationId);
            }
            DockingDirection? nodeDockingDirection = (await _agvDb.Graphs.GetNodeAsync(targetNodeId)).DockingDirection;
            actionGroup.SetDockFrontOfAction(currentAction.CorrelationId, previousActionGroupDockFront, nodeDockingDirection, startNodeIsStation);
            var targetNode = await _agvDb.Graphs.GetNodeAsync(targetNodeId);
            actionGroup.SetCargoLiftHeightForAction(currentAction.CorrelationId, targetNode.PickLiftHeight, targetNode.DropLiftHeight);
        }

        actionGroup.SetTargetForAction(actionId, targetNodeId, shouldUpdate ? route : null);

        await _actionGroupRepository.UpdateAsync(actionGroup);

        if (assignedAgv != null)
        {
            if (shouldUpdate)
            {
                var mappedAction = _mapper.Map<MessageAgvAction>(currentAction);
                await context.Publish(new UpdateActionCommand((Guid)assignedAgv, agv!.SerialNumber, mappedAction), cancellationToken);
            }
            else
            {
                await context.Publish(new AgvBecameIdle(assignedAgv.Value), cancellationToken);
            }
        }
        await context.Publish(new ActionGroupChanged(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId), cancellationToken);

        _logger.LogOrderInformation(LogCodes.OrderIncomplete, actionGroup.AssignedAgv.GetValueOrDefault(),
            actionGroup.CorrelationId, "Action Target Set",
            $"Target {targetNodeId} set for action {nextAction.ActionType} of order {actionGroup.OrderId}",
            new Dictionary<string, object?>
            {
                { LogLabels.NodeId, targetNodeId },
                { LogLabels.ActionType, nextAction.ActionType.ToString() },
                { LogLabels.OrderId, actionGroup.OrderId }
            });
    }

    private async Task<bool> GetStartNodeIsStation(AgvAction currentAction)
    {
        return currentAction.Nodes.Length >= 1 && (await _agvDb.Graphs.GetNodeAsync(currentAction.Nodes.First())).IsStation();
    }

    /// <summary>
    /// Handles the <see cref="CancelOrAbortActionGroup"/>
    /// </summary>
    public async Task Consume(ConsumeContext<CancelOrAbortActionGroup> context)
    {
        var actionGroupId = context.Message.CorrelationId;
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupId);
        Guid? agvCorrelationId = actionGroup.AssignedAgv;
        if (agvCorrelationId == null)
        {
            await context.Publish(new CancelActionGroup(actionGroupId, CancelReason.External));
            return;
        }
        var assignedAgv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId.Value);
        //if action group was queueing order, set AGV free of queuing
        if (actionGroup.OrderId.Equals(QueuingOrderId))
        {
            assignedAgv.IsQueueing = false;
            await _agvDb.Agvs.UpdateAgvAsync(assignedAgv);
        }
        await context.Publish(new AbortActionGroup(actionGroupId, AbortReason.External));
    }

    /// <summary>
    /// Handles the <see cref="SaveActionGroupAbortion"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SaveActionGroupAbortion> context)
    {
        var (_, abortedActionId, reason) = context.Message;
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByActionIdAsync(abortedActionId);

        var abortReason = reason.ToReason();
        actionGroup.AbortAction(abortedActionId, abortReason != null ? [abortReason] : []);

        await _actionGroupRepository.UpdateAsync(actionGroup);

        await context.Publish(new AutomaticActionWithdrawn(actionGroup.AssignedAgv, actionGroup.Actions.OrderBy(a => a.Sequence).First().Target.NodeId!, true));
        await context.Publish(new ActionGroupChanged(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId));

        _logger.LogOrderWarning(LogCodes.OrderAborted, actionGroup.AssignedAgv.GetValueOrDefault(),
            actionGroup.CorrelationId, "Order Aborted",
            $"Order {actionGroup.OrderId} was aborted due to {reason}",
            new Dictionary<string, object?>
            {
                { LogLabels.OrderId, actionGroup.OrderId },
                { LogLabels.Reason, reason },
                { LogLabels.ReasonDescription, reason }
            });
        await _metricCollectionService.CollectOrderCompletionMetrics(actionGroup);
    }

    /// <summary>
    /// Handles the <see cref="TryUpdateIdleAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryUpdateIdleAction> context)
    {
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(context.Message.CorrelationId);
        string targetNodeId = actionGroup.Actions.Single(a => a.IsFirstAction).Target.NodeId!;

        await _actionManagerSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            // Get and select agv
            //Get a list of all action groups that are started but idling
            List<Domain.Models.ActionGroup.ActionGroup> idleActionGroups = (await _actionGroupRepository.GetRunningIdleActionGroups()).ToList();
            //Get AGV that handles these already started action groups
            List<Guid> idleAgvCorrelationIds = idleActionGroups.Select(a => a.AssignedAgv!.Value).ToList();
            _logger.LogInformation("There are AGVs {ListOfAgvs} with started action groups", string.Join(',', idleAgvCorrelationIds));

            //Get all AGVs in queue heads
            ICollection<Guid> agvsAtQueueHeads = await _queueManager.GetAgvsAtQueueHeads();

            //Get all ready AGV
            ICollection<Agv> readyAgvs = await _agvDb.Agvs.GetReadyAgvsWithAtLeastLowBatteryLevel();
            List<Guid> readyAgvsList = readyAgvs.Select(a => a.CorrelationId).ToList();
            _logger.LogInformation("There are AGVs {ListOfAgvs} that are ready", string.Join(',', readyAgvsList));

            List<Agv> agvs = readyAgvs.ToList();

            //if there are already started action groups, and the last assigned AGV to this group is not empty
            if (idleActionGroups.Count != 0)
            {
                ICollection<Agv> idleAgvs = await _agvDb.Agvs.SelectAgvsWithAboveCriticalBatteryLevel(idleAgvCorrelationIds);
                //add those AGV to the list of ready AGV
                agvs.AddRange(idleAgvs);
            }

            //Get infinitely charging agvs that reached min charge up
            ICollection<Domain.Models.ActionGroup.ActionGroup> chargingActionGroups = await _actionGroupRepository.GetRunningChargeActionGroups();
            List<Guid> chargingAgvCorrelationIds = chargingActionGroups.Select(a => a.AssignedAgv!.Value).ToList();
            List<Guid> infinitelyChargingAgvsList = [];
            if (chargingAgvCorrelationIds.Count != 0)
            {
                ICollection<Agv> infinitelyChargingAgvs = await _agvDb.Agvs.SelectInfinitelyChargingAgvsWithAboveMinChargeUpBatteryLevel(chargingAgvCorrelationIds);
                infinitelyChargingAgvsList = infinitelyChargingAgvs.Select(a => a.CorrelationId).ToList();
                _logger.LogInformation("There are AGVs {ListOfAgvs} that are infinitely charging and have a battery level above min charge up", string.Join(',', infinitelyChargingAgvsList));
                chargingActionGroups = chargingActionGroups.Where(a => infinitelyChargingAgvsList.Contains(a.AssignedAgv.GetValueOrDefault())).ToList();
                idleActionGroups.AddRange(chargingActionGroups);

                agvs.AddRange(infinitelyChargingAgvs);
            }

            //if the list of AGV at a queue head is not empty
            if (agvsAtQueueHeads.Count > 0)
            {
                _logger.LogInformation("There are AGVs {ListOfAgvs} at queue heads, adding them", string.Join(',', agvsAtQueueHeads));

                List<Guid> missingAgvsFromQueueHead = agvsAtQueueHeads.Where(q => !readyAgvs.Select(a => a.CorrelationId).Contains(q)).ToList();
                if (missingAgvsFromQueueHead.Count != 0)
                {
                    List<Agv> queueingAgvs = (await _agvDb.Agvs.SelectExistingAgvsAsync(missingAgvsFromQueueHead)).ToList();
                    //add those AGV to the list of ready AGV
                    agvs.AddRange(queueingAgvs);
                }
            }

            //remove duplicates if any
            _logger.LogInformation("There are AGVs {ListOfAgvs} that are ready before removing duplicates", string.Join(',', agvs.Select(a => a.CorrelationId).ToList()));
            agvs = agvs.DistinctBy(a => a.CorrelationId).OrderBy(a => a.CorrelationId).ToList();
            _logger.LogInformation("There are AGVs {ListOfAgvs} that are ready after removing duplicates", string.Join(',', agvs.Select(a => a.CorrelationId).ToList()));

            //Remove agvs with errors
            IEnumerable<Guid> agvsWithErrors = (await _agvDb.Errors.GetActiveErrorsAsync(agvs.Select(a => a.CorrelationId).ToList())).Where(e => e.ErrorLevel == ErrorLevel.Fatal).Select(e => e.AgvCorrelationId).Distinct();
            agvs = agvs.Where(a => !agvsWithErrors.Contains(a.CorrelationId)).OrderBy(a => a.CorrelationId).ToList();
            _logger.LogInformation("There are AGVs {ListOfAgvs} that are ready after removing agvs with errors ", string.Join(',', agvs.Select(a => a.CorrelationId).ToList()));

            //Remove AGV that have a priority action group assigned, e.g. manual moves with priority
            ICollection<Guid> agvsWithPriorityActionGroups = await _actionGroupRepository.FilterAgvsWithPriorityActionGroups(agvs.Select(a => a.CorrelationId).ToList());
            agvs = agvs.Where(a => !agvsWithPriorityActionGroups.Contains(a.CorrelationId)).OrderBy(a => a.CorrelationId).ToList();
            _logger.LogInformation("There are AGVs {ListOfAgvs} that are left after removing prio actions", string.Join(',', agvs.Select(a => a.CorrelationId).ToList()));

            if (agvs.Count == 0)
            {
                _logger.LogInformation("No agv available for reactivating idle action");
                return;
            }

            //get list of all AGV data from the available AGV
            List<AgvData> agvsData = (await _agvDb.Data.GetSelectedAgvDataAsync(agvs.Select(a => a.CorrelationId).ToList())).OrderBy(a => a.CorrelationId).ToList();

            //Create tuples of AGV and route costs to action group.
            //Skip agvs that are on the target or on the first edge to the next waypoint,
            //as it could already be moving away and running a detour to the target.
            (Guid? agv, Route? route) closestAgvRoute = await _routeService.GetAgvRouteClosestToTarget((ICollection<(Agv agv, AgvData? agvData)>)agvs.Zip(agvsData).ToList(), targetNodeId, true);
            if (closestAgvRoute.agv == null)
            {
                _logger.LogInformation("Reactivate all parking agvs and agvs at queue heads");
                foreach (var agv in agvsAtQueueHeads)
                {
                    await context.Publish(new AgvBecameIdle(agv));
                }
                foreach (var agv in readyAgvs)
                {
                    await context.Publish(new AgvBecameIdle(agv.CorrelationId));
                }
                return;
            }

            if (!idleAgvCorrelationIds.Contains(closestAgvRoute.agv.Value) && !infinitelyChargingAgvsList.Contains(closestAgvRoute.agv.Value))
            {
                _logger.LogInformation("Reactivate agv {AgvCorrelationId}", closestAgvRoute.agv);
                await context.Publish(new AgvBecameIdle(closestAgvRoute.agv.Value));
                return;
            }
            _logger.LogInformation("Reactivate agv {AgvCorrelationId} that is running idle action", closestAgvRoute.agv);

            idleActionGroups = idleActionGroups.DistinctBy(a => a.CorrelationId).ToList();

            var abortedActionGroup = idleActionGroups.Single(a => a.AssignedAgv == closestAgvRoute.agv.Value);
            var abortedAction = abortedActionGroup.Actions.Single(a => a.State == ActionState.Started);

            // The start of the move action, the docking direction of the last completed action,
            // and the current nodes are needed 
            // to deduce the docking direction as if the updated action starts where the to be aborted move started.
            // The docking direction of the aborted move is not taken as the previous value, as the agvs are configured to
            // turn as later as possible, i.e. likely not have turned yet.
            bool startNodeIsStation = await GetStartNodeIsStation(abortedAction);
            var actionToStart = actionGroup.FirstNotStartedAction!;
            var targetNode = await _agvDb.Graphs.GetNodeAsync(targetNodeId);
            DockingDirection? nodeDockingDirection = targetNode.DockingDirection;
            actionGroup.AssignedAgv = closestAgvRoute.agv;
            bool? previousActionGroupDockFront = await _actionGroupRepository.GetDockFrontOfLastCompletedAction(closestAgvRoute.agv.Value, actionGroup.CorrelationId);
            actionGroup.StartAction(actionToStart.CorrelationId, closestAgvRoute!.route!.Nodes, closestAgvRoute.route.Edges);
            actionGroup.HasPriority = true;
            actionGroup.SetDockFrontOfAction(actionToStart.CorrelationId, previousActionGroupDockFront, nodeDockingDirection, startNodeIsStation);

            // Get route costs
            var assignedAgv = agvs.Single(a => a.CorrelationId == actionGroup.AssignedAgv!.Value);
            double duration = closestAgvRoute.route.Costs;
            duration += await _durationEstimationService.AddNonRouteCosts(false, actionGroup, actionToStart, assignedAgv);
            actionGroup.EstimatedDuration = TimeSpan.FromSeconds(duration);

            await _actionService.TryUpdateActionGroup(actionGroup);

            // Publish messages
            await context.Publish(new AbortAndStartActionCommand(assignedAgv.CorrelationId,
                assignedAgv.SerialNumber,
                _mapper.Map<MessageAgvAction>(actionToStart), abortedAction.CorrelationId));

            await context.Publish(new ActionGroupStarted(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId, DateTime.UtcNow));
            await context.Publish(new SaveActionGroupAbortion(abortedActionGroup.CorrelationId, abortedAction.CorrelationId, abortedActionGroup.IsChargingOrder ? AbortReason.ReactivatedFromCharging : AbortReason.ReactivatedFromParking));

            // Log for ui
            _logger.LogOrderInformation(
                LogCodes.OrderStarted,
                actionGroup.AssignedAgv.GetValueOrDefault(),
                actionGroup.CorrelationId,
                "Order Started",
                $"Order {actionGroup.OrderId} has been started",
                new Dictionary<string, object?>
                {
                    { LogLabels.OrderId, actionGroup.OrderId }
                });
            _logger.LogOrderInformation(
                LogCodes.OrderActionStarted,
                actionGroup.AssignedAgv.GetValueOrDefault(),
                actionGroup.CorrelationId,
                "Order Updated",
                $"{actionToStart.ActionType} of order {actionGroup.OrderId} has been started",
                new Dictionary<string, object?>
                {
                    { LogLabels.OrderId, actionGroup.OrderId },
                    { LogLabels.ActionType, actionToStart.ActionType }
                });
            await _actionService.LogStationUpdate(assignedAgv, actionToStart, actionGroup);
        }, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="ReassignActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ReassignActionGroup> context)
    {
        await _actionManagerSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(context.Message.CorrelationId);
            var actionToAbort = actionGroup.Actions.Single(a => a.State is ActionState.Started or ActionState.Paused);

            var assignedAgv = actionGroup.AssignedAgv!.Value;

            actionGroup.ResetActionGroup();
            actionGroup.Queue();
            actionGroup.HasPriority = true;

            var agvToAssign = context.Message.AssignedAgv != null ? await _agvDb.Agvs.GetAgvAsync(context.Message.AssignedAgv.Value) : null;
            if (agvToAssign == null)
            {
                ICollection<Agv> readyAgvs = await _agvDb.Agvs.GetReadyAgvsWithAtLeastLowBatteryLevel();
                readyAgvs = readyAgvs.Where(a => a.CorrelationId != assignedAgv).ToList();

                List<AgvData> agvsData = (await _agvDb.Data.GetSelectedAgvDataAsync(readyAgvs.Select(a => a.CorrelationId).ToList())).OrderBy(a => a.CorrelationId).ToList();

                (Guid? agv, Route? route) closestAgvRoute = await _routeService.GetAgvRouteClosestToTarget((ICollection<(Agv agv, AgvData? agvData)>)readyAgvs.Zip(agvsData).ToList(), actionGroup.FirstNotStartedAction!.Target.NodeId!);

                if (closestAgvRoute.agv == null)
                {
                    await _actionService.TryUpdateActionGroup(actionGroup);
                    _logger.LogOrderWarning(LogCodes.OrderReset, actionGroup.AssignedAgv.GetValueOrDefault(),
                        actionGroup.CorrelationId, "Order Reset", $"Order {actionGroup.OrderId} was reset ",
                        new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId } });
                    await context.Publish(new ActionGroupCreated(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId, null));
                    return;
                }
                agvToAssign = readyAgvs.Single(a => a.CorrelationId == closestAgvRoute.agv!.Value);
            }

            actionGroup.Assign(agvToAssign);
            actionGroup.IsManual = true;

            await _actionService.TryUpdateActionGroup(actionGroup);

            await context.Publish(new AgvBecameIdle(agvToAssign.CorrelationId));

            await context.Publish(new ActionAborted(assignedAgv, _mapper.Map<Common.Messages.ActionGroups.AgvAction>(actionToAbort)));

            await context.Publish(new ActionGroupChanged(_mapper.Map<ActionGroup>(actionGroup), actionGroup.CorrelationId));

            await context.Publish(new ActionGroupReassignedOrReset(actionGroup.CorrelationId, actionGroup.Creator));

            _logger.LogOrderInformation(
                LogCodes.OrderReassigned,
                actionGroup.AssignedAgv.GetValueOrDefault(),
                actionGroup.CorrelationId,
                "Order Updated",
                $"Order {actionGroup.OrderId} has been reassigned to agv {agvToAssign.SerialNumber} ",
                new Dictionary<string, object?>
                {
                    { LogLabels.OrderId, actionGroup.OrderId },
                    { LogLabels.AgvSerialNumber, agvToAssign.SerialNumber }
                });
        }, context.CancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="ForceQueueActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ForceQueueActionGroup> context)
    {
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(context.Message.CorrelationId);
        actionGroup.Queue();
        await _actionService.TryUpdateActionGroup(actionGroup);

        var actionGroupMessageContent = _mapper.Map<ActionGroup>(actionGroup);

        _logger.LogOrderInformation(LogCodes.OrderQueued, actionGroupMessageContent.AssignedAgv.GetValueOrDefault(),
            actionGroupMessageContent.CorrelationId, "Order Queued", $"Order {actionGroupMessageContent.OrderId} has been queued",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroupMessageContent.OrderId } });

        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), context.CancellationToken);
        await context.Publish(new ActionGroupQueued(actionGroupMessageContent, actionGroup.CorrelationId, actionGroup.AssignedAgv), context.CancellationToken);
    }
}