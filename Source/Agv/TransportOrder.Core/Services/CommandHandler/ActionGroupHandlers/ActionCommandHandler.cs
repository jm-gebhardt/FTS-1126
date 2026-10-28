using AutoMapper;
using FluentValidation;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.HostOrder.Commands;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using ActionGroupCreated = Gebhardt.Acc.Common.Messages.ActionGroups.Events.ActionGroupCreated;
using Agv = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.Agv;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;
using AgvData = Gebhardt.Acc.Common.Domain.Models.AgvData;
using MessageAgvAction = Gebhardt.Acc.Common.Messages.ActionGroups.AgvAction;
using Route = Gebhardt.Acc.TransportOrder.Routing.Models.Route;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler.ActionGroupHandlers;

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
    IConsumer<ForceQueueActionGroup>,
    IConsumer<TryRetractActionGroup>,
    IConsumer<HandleActionFailure>
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
    private readonly ActionCommandHelper _actionCommandHelper;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly TimeProvider _timeProvider;
    private readonly IQueueManager _queueManager;
    private readonly ILoggingService _loggingService;
    private readonly INodeRepository _nodeRepository;

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
        IMapper mapper,
        IBus bus,
        ILogger<ActionCommandHandler> logger,
        ActionCommandHelper actionCommandHelper,
        IPublishEndpoint publishEndpoint,
        TimeProvider timeProvider,
        ILoggingService loggingService,
        IRequestClient<RetractHostOrder> retractHostOrderClient,
        INodeRepository nodeRepository)
    {
        _actionCommandHelper = actionCommandHelper;
        _publishEndpoint = publishEndpoint;
        _timeProvider = timeProvider;
        _logger = logger;
        _routeService = routeService;
        _agvDb = agvDb;
        _actionGroupRepository = actionGroupRepository;
        _chooseAgvActionGroupService = chooseAgvActionGroupService;
        _actionManagerSemaphoreProvider = actionManagerSemaphoreProvider;
        _actionGroupValidationService = actionGroupValidationService;
        _actionGroupRetrievalService = actionGroupRetrievalService;
        _durationEstimationService = durationEstimationService;
        _loggingService = loggingService;
        _queueManager = queueManager;
        _mapper = mapper;
        _bus = bus;
        _nodeRepository = nodeRepository;
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
        bool agvActionCanBeStarted = await _actionCommandHelper.CheckAgvActionCanBeStarted(context, action, assignedAgv, actionGroup);
        if (agvActionCanBeStarted)
        {
            bool? previousActionGroupDockFront = null;
            if (action.IsFirstAction ||  actionGroup.Actions.OrderBy(a => a.Sequence).LastOrDefault(a => !a.ShouldSkip && a.Sequence < action.Sequence) == null)
            {
                previousActionGroupDockFront = await _actionGroupRepository.GetDockFrontOfLastCompletedAction(agvCorrelationId, actionGroup.CorrelationId);
            }
            var targetNode = actionGroup.Actions.Single(a => a.CorrelationId == action.CorrelationId).Target.Node;

            targetNode = targetNode != null ? await _agvDb.Graphs.GetNodeAsync(targetNode.Id) : null;
            DockingDirection? nodeDockingDirection = targetNode?.DockingDirection;
            bool startNodeIsStation = await GetStartNodeIsStation(action);
            // ensure nodes are set before DockFront is diagnosed, to allow identification of Doppelspiel
            actionGroup.StartAction(action.CorrelationId, action.Nodes, action.Edges, _timeProvider.GetUtcNow());
            actionGroup.SetDockFrontOfAction(action.CorrelationId, previousActionGroupDockFront, nodeDockingDirection, startNodeIsStation);
            actionGroup.SetCargoLiftHeightForAction(action.CorrelationId, targetNode?.PickLiftHeight, targetNode?.DropLiftHeight);
            await _actionCommandHelper.TryUpdateActionGroup(actionGroup);
            action = actionGroup.Actions.Single(a => a.CorrelationId == action.CorrelationId);

            var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
            await context.Publish(new StartActionCommand(assignedAgv.CorrelationId, assignedAgv.SerialNumber, _mapper.Map<MessageAgvAction>(action)), cancellationToken);
            await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);
            if (action.IsFirstAction)
            {
                await _loggingService.LogOrder(
                    _logger,
                    LogLevel.Information,
                    LogCodes.OrderStarted,
                    actionGroup.AssignedAgv.GetValueOrDefault(),
                    actionGroup.CorrelationId,
                    "Order Started",
                    $"Order {actionGroup.OrderId} has been started",
                    new Dictionary<string, object?>
                    {
                        { LogLabels.OrderId, actionGroup.OrderId }
                    });
                await context.Publish(new ActionGroupStarted(actionGroupMessageContent, actionGroup.CorrelationId, _timeProvider.GetUtcNow().UtcDateTime), cancellationToken);
            }

            if (action.ActionType == ActionType.Pick)
            {
                await context.Publish(new AgvStartedPick(action.Target.Node!.Name, action.ParentId), cancellationToken);
            }

            await _loggingService.LogOrder(
                _logger,
                LogLevel.Information,
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
            _logger.LogInformation("{ActionType} for action group {CorrelationId} has started", action.ActionType, actionGroup.CorrelationId);
            await _actionCommandHelper.LogStationUpdate(assignedAgv, action, actionGroup);
        }
    }

    /// <summary>
    /// Handles the <see cref="EstimateActionGroupDuration"/> command.
    /// </summary>    
    public async Task Consume(ConsumeContext<EstimateActionGroupDuration> context)
    {
        var agvId = context.Message.AgvCorrelationId;
        var action = context.Message.Action;
        var actionGroupId = context.Message.ActionGroupId;

        Task<Domain.Models.ActionGroup.ActionGroup> actionGroupTask = _actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupId);
        Task<Agv> agvTask = _agvDb.Agvs.GetAgvAsync(agvId);
        Task<AgvData?> agvDataTask = _agvDb.Data.GetAgvDataAsync(agvId);
        var actionGroup = await actionGroupTask;

        if (actionGroup.EstimatedDuration != null)
        {
            // Duration has already been estimated (race condition) so return to break parallel execution.
            return;
        }

        await Task.WhenAll(actionGroupTask, agvTask, agvDataTask);
        var assignedAgv = agvTask.Result;
        var agvData = agvDataTask.Result;
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

        await _actionCommandHelper.TryUpdateActionGroup(actionGroup);
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
            _logger.LogWarning("No succeeding action for agv with CorrelationId: {AgvCorrelationId}", agv.CorrelationId);
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
                _logger.LogDebug("Not starting new action for {ActionGroupCorrelationId} for agv with CorrelationId: {AgvCorrelationId} because all remaining actions were marked to be skipped", actionGroupId, agv.CorrelationId);
                return;
            }

            _logger.LogDebug("Starting succeeding action {Action} of group {ActionGroupCorrelationId} for agv with CorrelationId: {AgvCorrelationId}", action.CorrelationId, actionGroupId, agv.CorrelationId);
            if (action.IsIncomplete)
            {
                _logger.LogDebug("Succeeding action {Action} of group {ActionGroupCorrelationId} is incomplete, thus not started for agv with CorrelationId: {AgvCorrelationId}", action.CorrelationId, actionGroupId, agv.CorrelationId);
                await context.Publish(new StartingActionForAgvFailed(agvCorrelationId), cancellationToken);
                return;
            }

            if (action.State == ActionState.Paused)
            {
                _logger.LogDebug("Succeeding action {Action} of group {ActionGroupCorrelationId} is paused, thus not started for agv with CorrelationId: {AgvCorrelationId}", action.CorrelationId, actionGroupId, agv.CorrelationId);
                return;
            }

            if (!await _actionCommandHelper.SelectNodesForAgvAction(action, agv, true, agv.HasError, cancellationToken))
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
            var manualAction = await _actionCommandHelper.GetManualAction(agv);
            if (manualAction == null)
            {
                _logger.LogInformation("There is no un-started manual action available for agv with CorrelationId {AgvCorrelationId}", agv.CorrelationId);
                return;
            }
            if (await _actionCommandHelper.SelectNodesForAgvAction(manualAction, agv, false, true, context.CancellationToken))
            {
                await context.Publish(new NodesSelectedForAction(manualAction, agv.CorrelationId), context.CancellationToken);
            }
            return;
        }

        await _actionManagerSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            // When checking to see if a new action group can be started, make sure to check the battery levels of the AGV. Do not
            // proceed if the AGV is in critical condition unless there is already existing charging action group because that will be
            // picked and prioritized in _chooseAgvActionGroupService. This is required because the chain of events that create a
            // charging action group might miss a window to start charging before the next action group is assigned to an idle AGV and
            // the AGV battery dies before moving to the charger is complete.
            var agvSettings = _agvDb.AgvSettings.GetSettingsForAgvGroup(agv.AgvSettingsGroup);
            bool agvMustCharge = agv.CheckMustCharge(agvSettings);
            bool hasChargingActionGroup = await _actionGroupRepository.HasCreatedChargingActionGroupForAgvAsync(agvCorrelationId);
            bool hasUnfinishedActionGroup = await _actionGroupRepository.HasUnfinishedActionGroupAsync(agv.CorrelationId);
            bool canStartNewAction = !agv.NeedsMaintenance && !hasUnfinishedActionGroup && !agv.IsLocked && (!agvMustCharge || hasChargingActionGroup);
            if (!canStartNewAction)
            {
                _logger.LogWarning(
                    "Can not start new action group for AGV {AgvCorrelationId} (is locked: {IsLocked}/ has unfinished action group: {HasUnfinishedActionGroup}/ needs maintenance: {NeedsMaintenance}/ AGV must charge: {AgvMustCharge}/ AGV has charging action group: {HasChargingActionGroup})",
                    agv.CorrelationId,
                    agv.IsLocked,
                    hasUnfinishedActionGroup,
                    agv.NeedsMaintenance,
                    agvMustCharge,
                    hasChargingActionGroup);
                return;
            }

            var agvData = await _agvDb.Data.GetAgvDataAsync(agv.CorrelationId);
            var chosenActionGroup = await _chooseAgvActionGroupService.GetNotStartedActionGroupAsync(agv, agvData, cancellationToken);

            if (chosenActionGroup == null)
            {
                _logger.LogInformation("There is no next action available for agv with CorrelationId {AgvCorrelationId}", agv.CorrelationId);
                await context.Publish(new AgvCanStartIdleAction(agv.CorrelationId), cancellationToken);
                return;
            }

            var currentAssignedActionGroup = await _actionGroupRepository.GetCurrentAssignedActionGroupAsync(agv.CorrelationId);
            if (currentAssignedActionGroup != null && chosenActionGroup.CorrelationId != currentAssignedActionGroup.CorrelationId)
            {
                _logger.LogInformation("Skip starting action group {ChosenActionGroupCorrelationId} for agv {AgvCorrelationId} because another action group {CurrentAssignedActionGroupCorrelationId} is already assigned",
                    chosenActionGroup.CorrelationId, agv.CorrelationId, currentAssignedActionGroup.CorrelationId);
                return;
            }

            var action = chosenActionGroup.FirstNotStartedAction!;
            _logger.LogDebug("Trying to start action {Action} of group {ActionGroupCorrelationId} with type {ActionType}", action.CorrelationId, action.ParentId, Enum.GetName(action.ActionType));

            if (await _actionCommandHelper.SelectNodesForAgvAction(action, agv, false, false, context.CancellationToken))
            {
                var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(chosenActionGroup.CorrelationId);
                if (actionGroup.AssignedAgv == null)
                {
                    actionGroup.Assign(agv.CorrelationId);
                    await _loggingService.LogOrder(
                        _logger,
                        LogLevel.Information, LogCodes.OrderAgvAssigned, agv.CorrelationId, actionGroup.CorrelationId,
                        "AGV Assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}",
                        new Dictionary<string, object?>
                        {
                            { LogLabels.OrderId, actionGroup.OrderId }, { LogLabels.AgvSerialNumber, agv.SerialNumber }
                        });

                    await _actionCommandHelper.TryUpdateActionGroup(actionGroup);
                }
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

        var actionGroupsByState = await _actionGroupRepository.GetLatestActionGroupsByStateForAgvAsync(agvCorrelationId);
        if (actionGroupsByState.Created != null || actionGroupsByState.Started is { IsChargingOrder: false } ) // allow creation of parking action group if agv is charging to ensure swapping of critical and charging agv works
        {
            _logger.LogInformation("Aborting creation of parking action for agv {AgvCorrelationId}, other action group created or started in the meantime", agvCorrelationId);
            await context.Publish(new FreeNode(parkNode.Id, true), cancellationToken);
            return;
        }
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        var actionGroup = new Domain.Models.ActionGroup.ActionGroup(
            groupCorrelationId,
            string.Empty,
            actions: new List<AgvAction> { new(Guid.NewGuid(), ActionType.Move, parkNode, groupCorrelationId) },
            assignedAgv: agv.CorrelationId,
            creator: ActionGroupCreator.TransportOrder,
            priority: Priority.Default,
            isQueued: true,
            doNotPause: true
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
        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderCreated, agv.CorrelationId, actionGroup.CorrelationId, "Order Created", $"Order {actionGroup.OrderId} was created successfully",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId } });
        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderAgvAssigned, agv.CorrelationId, actionGroup.CorrelationId, "AGV Assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId }, { LogLabels.AgvSerialNumber, agv.SerialNumber } });

        if (!await _actionGroupRepository.AddAsync(actionGroup))
        {
            _logger.LogError("Failed to save action group {ActionGroupCorrelationId}", actionGroup.CorrelationId);
            throw new ActionsInActionGroupEmptyException(actionGroup.CorrelationId);
        }
        await context.Publish(new ActionGroupCreated(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), actionGroup.CorrelationId, agv.CorrelationId), cancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="CreateQueueingActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateQueueingActionGroup> context)
    {
        var (groupCorrelationId, agvCorrelationId, queueNode) = context.Message;
        var cancellationToken = context.CancellationToken;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        var actionGroup = new Domain.Models.ActionGroup.ActionGroup(
            groupCorrelationId,
            QueuingOrderId,
            actions: new List<AgvAction> { new(Guid.NewGuid(), ActionType.Move, queueNode, groupCorrelationId) },
            assignedAgv: agv.CorrelationId,
            creator: ActionGroupCreator.TransportOrder,
            priority: Priority.Queueing,
            isQueued: true,
            doNotPause: true
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
        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderCreated, agv.CorrelationId, actionGroup.CorrelationId,
            "Order Created", $"Order {actionGroup.OrderId} was created successfully",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId } });
        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderAgvAssigned, agv.CorrelationId, actionGroup.CorrelationId,
            "AGV Assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId }, { LogLabels.AgvSerialNumber, agv.SerialNumber } });

        if (!await _actionGroupRepository.AddAsync(actionGroup))
        {
            _logger.LogError("Failed to save action group {ActionGroupCorrelationId}", actionGroup.CorrelationId);
            throw new ActionsInActionGroupEmptyException(actionGroup.CorrelationId);
        }
        await context.Publish(new ActionGroupCreated(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), actionGroup.CorrelationId, agv.CorrelationId), cancellationToken);
    }

    /// <summary>
    /// Handles the <see cref="CreateChargingActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CreateChargingActionGroup> context)
    {
        var (groupCorrelationId, agvCorrelationId, chargeStation, initiator) = context.Message;
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
                    new(Guid.NewGuid(), ActionType.Move, new GraphNode(chargeStation.Id, chargeStation.Name), groupCorrelationId)
                    {
                        Sequence = 0,
                        InfoToAgv = new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.DockFront, true.ToString() } }
                    },
                    new(Guid.NewGuid(), ActionType.Charge, new GraphNode(chargeStation.Id, chargeStation.Name), groupCorrelationId)
                    {
                        Sequence = 1,
                        InfoToAgv = new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.DockFront, true.ToString() } }
                    }
                },
                assignedAgv: agv.CorrelationId,
                creator: initiator,
                priority: Priority.Charging,
                shouldEscapeQueue: true,
                isManual: triggeredByWebApi,
                isQueued: true,
                doNotPause: !triggeredByWebApi
            );

            if (!await _actionGroupRepository.AddAsync(actionGroup))
            {
                _logger.LogError("Failed to save action group {ActionGroupCorrelationId}", actionGroup.CorrelationId);
                throw new ActionsInActionGroupEmptyException(actionGroup.CorrelationId);
            }
            await _publishEndpoint.Publish(new ActionGroupCreated(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), actionGroup.CorrelationId, agv.CorrelationId), cancellationToken);
            await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderCreated, agv.CorrelationId, actionGroup.CorrelationId,
                "Order Created", $"Order {actionGroup.OrderId} was created successfully",
                new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId } });
            await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderAgvAssigned, agv.CorrelationId, actionGroup.CorrelationId,
                "AGV Assigned", $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}",
                new Dictionary<string, object?>
                    { { LogLabels.OrderId, actionGroup.OrderId }, { LogLabels.AgvSerialNumber, agv.SerialNumber } });
        }
    }

    private async Task<Domain.Models.ActionGroup.ActionGroup> ValidDateActionGroup(Domain.Models.ActionGroup.ActionGroup actionGroup, bool throwOnInvalid)
    {
        _logger.LogTrace("Validating ActionGroup with CorrelationId: {ActionGroupCorrelationId}", actionGroup.CorrelationId);
        var validationResult = await _actionGroupValidationService.ValidateAsync(actionGroup);
        bool isValid = validationResult.IsValid;

        if (!isValid)
        {
            _logger.LogError("Validation result of ActionGroup with CorrelationId: {ActionGroupCorrelationId} is not valid! Errors: {Errors}", actionGroup.CorrelationId, validationResult);
            if (throwOnInvalid)
            {
                throw new ValidationException(validationResult.Errors);
            }
            actionGroup.InvalidateActionGroup(validationResult.ToReasons(), _timeProvider.GetUtcNow());
            var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
            List<string> validationFailureReasons = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            await _loggingService.LogOrder(_logger, LogLevel.Warning, LogCodes.OrderInvalid, actionGroupMessageContent.AssignedAgv.GetValueOrDefault(),
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
        actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
        if (!updated)
        {
            await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderCreated, actionGroupMessageContent.AssignedAgv.GetValueOrDefault(),
                actionGroupMessageContent.CorrelationId, "Order Created", $"Order {actionGroupMessageContent.OrderId} was created successfully",
                new Dictionary<string, object?> { { LogLabels.OrderId, actionGroupMessageContent.OrderId } });

            await _bus.Publish(new ActionGroupCreated(actionGroupMessageContent, actionGroupMessageContent.CorrelationId, actionGroupMessageContent.AssignedAgv), cancellationToken);
        }

        if (actionGroup.IsQueued)
        {
            await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderQueued, actionGroupMessageContent.AssignedAgv.GetValueOrDefault(),
                actionGroupMessageContent.CorrelationId, "Order Queued", $"Order {actionGroupMessageContent.OrderId} has been queued",
                new Dictionary<string, object?> { { LogLabels.OrderId, actionGroupMessageContent.OrderId } });
            await _bus.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), cancellationToken);
            if (updated)
            {
                await context.Publish(new ActionGroupQueued(actionGroupMessageContent, actionGroup.CorrelationId, actionGroup.AssignedAgv), context.CancellationToken);
            }
        }

        if (actionGroupMessageContent.AssignedAgv != null && actionGroup.IsQueued)
        {
            var agv = await _agvDb.Agvs.GetAgvAsync(actionGroupMessageContent.AssignedAgv.Value);
            await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderAgvAssigned, actionGroupMessageContent.AssignedAgv.Value,
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

        var actionGroup = await _actionGroupRepository.GetActionGroupAsync(actionGroupMessageContent.CorrelationId);
        if (actionGroup != null)
        {
            _logger.LogWarning("Action group {ActionGroupCorrelationId} already exists, not creating it again", actionGroupMessageContent.CorrelationId);
            return;
        }

        actionGroup = _mapper.Map<Domain.Models.ActionGroup.ActionGroup>(actionGroupMessageContent);

        actionGroup = await ValidDateActionGroup(actionGroup, false);

        bool updated = await _actionGroupRepository.AddOrUpdateAsync(actionGroup);
        actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
        if (!updated)
        {
            await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderCreated, actionGroupMessageContent.AssignedAgv.GetValueOrDefault(),
                actionGroupMessageContent.CorrelationId, "Order Created", $"Order {actionGroupMessageContent.OrderId} was created successfully",
                new Dictionary<string, object?> { { LogLabels.OrderId, actionGroupMessageContent.OrderId } });

            await _bus.Publish(new ActionGroupCreated(actionGroupMessageContent, actionGroupMessageContent.CorrelationId, actionGroupMessageContent.AssignedAgv), cancellationToken);
        }

        if (actionGroupMessageContent.AssignedAgv != null)
        {
            var agv = await _agvDb.Agvs.GetAgvAsync(actionGroupMessageContent.AssignedAgv.Value);
            await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderAgvAssigned, actionGroupMessageContent.AssignedAgv.Value,
                actionGroup.CorrelationId, "AGV Assigned",
                $"Order {actionGroup.OrderId} was assigned to {agv.SerialNumber}",
                new Dictionary<string, object?>
                    { { LogLabels.OrderId, actionGroup.OrderId }, { LogLabels.AgvSerialNumber, agv.SerialNumber } });
        }
    }

    /// <summary>
    /// Handles the <see cref="CompleteActionOfActionGroup"/> command.
    /// Ignores already completed actions without publishing completion side effects.
    /// </summary>
    public async Task Consume(ConsumeContext<CompleteActionOfActionGroup> context)
    {
        var correlationId = context.Message.ActionCorrelationId;
        var cancellationToken = context.CancellationToken;

        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByActionIdAsync(correlationId);
        var action = actionGroup.Actions.Single(a => a.CorrelationId == correlationId);
        if (context.Message.Revision < action.Revision)
        {
            _logger.LogWarning("Ignoring completion of action {ActionCorrelationId} in action group {ActionGroupCorrelationId} in outdated revision {Revision}, current revision is {CurrentRevision}",
                action.CorrelationId, actionGroup.CorrelationId, context.Message.Revision, action.Revision);
            return;
        }
        if (action.State == ActionState.Completed || action.CompletedDate != null)
        {
            _logger.LogInformation("Ignoring duplicate completion of action {ActionCorrelationId} in action group {ActionGroupCorrelationId}, completed at {CompletedDate}",
                action.CorrelationId, actionGroup.CorrelationId, action.CompletedDate);
            return;
        }

        var agvCorrelationId = actionGroup.AssignedAgv!.Value;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
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
        actionGroup.CompleteAction(correlationId, _timeProvider.GetUtcNow());
        await _actionCommandHelper.TryUpdateActionGroup(actionGroup);

        if (action.ActionType is ActionType.Pick or ActionType.Drop)
        {
            // Cargo error might be added already due to earlier mismatch
            await context.Publish(new CheckAgvLoad(actionGroup.AssignedAgv.Value), cancellationToken);
        }

        var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
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
            await _actionCommandHelper.TryRetractActionGroup(context, actionGroup, agv, cancellationToken);
        }

        if (action.ActionType == ActionType.Drop)
        {
            await _actionCommandHelper.TryQueueDoppelspielPartner(context, actionGroup, action, agv, cancellationToken);
            await context.Publish(new DropActionCompleted(actionGroup.CorrelationId, action.CorrelationId, actionGroupMessageContent, action.CompletedDate.GetValueOrDefault()), cancellationToken);
        }

        if (action.ActionType == ActionType.Charge)
        {
            await context.Publish(new ChargeActionCompleted(actionGroup.CorrelationId, action.CorrelationId), cancellationToken);
        }

        if (actionGroup.State != ActionGroupState.Completed)
        {
            await context.Publish(new NonChargingActionCompleted(agvCorrelationId), cancellationToken);
            await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderActionCompleted, actionGroup.AssignedAgv.GetValueOrDefault(),
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
        if (agv.IsQueueing && action.Target.Node != null)
        {
            bool isAgvAtQueueHead = await _queueManager.TryReachQueueNode(agv, action.Target.Node.Id);
            isQueueing = !isAgvAtQueueHead;
            await context.Publish(new TryEnqueueAgv(agv.CorrelationId, action.Target.Node.Id), cancellationToken);
        }

        if (agv.HasPendingModeSwitch && agv.Mode.CurrentActionGroupPolicy != CurrentActionGroupPolicy.Pause)
        {
            await context.Publish(new SwitchMode(agv.CorrelationId), cancellationToken);
        }
        await context.Publish(new NonChargingActionCompleted(agvCorrelationId, isQueueing), cancellationToken);
        await context.Publish(new ActionGroupCompleted(actionGroupMessageContent, actionGroup.CorrelationId, _timeProvider.GetUtcNow().UtcDateTime), cancellationToken);
        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderCompleted, actionGroup.AssignedAgv.GetValueOrDefault(),
            actionGroup.CorrelationId, "Order Completed", $"Order {actionGroup.OrderId} was completed successfully",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId } });
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
            _logger.LogError("Could not abort action group {ActionGroupCorrelationId}, it is not assigned to an agv", actionGroup.CorrelationId);
            await context.RespondAsync(new AbortActionGroupFailed("Action group abortion failed, no assigned agv."));
            return;
        }
        var action = actionGroup.Actions.FirstOrDefault(a => !a.ShouldSkip && a.State != ActionState.Completed);
        if (action == null)
        {
            _logger.LogError("Could not abort action group {ActionGroupCorrelationId}, no actions that are not completed", actionGroup.CorrelationId);
            await context.RespondAsync(new AbortActionGroupFailed("Action group abortion failed, all actions completed."));
            return;
        }
        var agv = await _agvDb.Agvs.GetAgvAsync((Guid) actionGroup.AssignedAgv!);
        actionGroup = await _actionGroupRepository.UpdateActionAndGroupStateAsync(action, agv, ActionState.Aborted, abortReason.ToReason(), _timeProvider.GetUtcNow());

        if (actionGroup is { IsQueueingOrder: true, OrderId: QueuingOrderId } && agv.IsQueueing)
        {
            if (await _queueManager.RemoveAgvFromQueueEntry(agv.CorrelationId, action.Target.NodeId!.Value))
            {
                await context.Publish(new LeaveQueue(agv.CorrelationId));
            }
        }

        await context.Publish(new ActionAborted(agv.CorrelationId, _mapper.Map<MessageAgvAction>(action)));

        if (agv.HasPendingModeSwitch)
        {
            await context.Publish(new SwitchMode(agv.CorrelationId));
        }

        var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));
        await context.RespondAsync(new Success());
        await _loggingService.LogOrder(_logger, LogLevel.Warning, LogCodes.OrderAborted, actionGroup.AssignedAgv.GetValueOrDefault(),
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
            var serviceNodeId = actionGroupWithNodes.Actions.First(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)).TargetNodeId!.Value;
            var serviceNode = await _agvDb.Graphs.GetNodeAsync(serviceNodeId);
            await context.Publish(new AutomaticActionWithdrawn(actionGroup.AssignedAgv, serviceNode, true));
        }
    }

    /// <summary>
    /// Handles the <see cref="CancelActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<CancelActionGroup> context)
    {
        (var actionGroupId, var cancelReason, string? user) = context.Message;
        var actionGroup = await _actionGroupRepository.GetActionGroupAsync(actionGroupId);

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
                await context.PublishAndRespond(new ActionGroupCancelFailed(actionGroupMessageContent, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderAlreadyCanceled));
                return;

            case ActionGroupState.Aborted:
                await context.Publish(new ActionGroupCancelFailed(actionGroupMessageContent, actionGroup.CorrelationId, HostOrderCancelFailedErrorCode.OrderAlreadyCanceled));
                return;
        }

        actionGroup = await _actionGroupRepository.UpdateActionAndGroupStateAsync(actionGroup.FirstNotStartedAction ?? actionGroup.Actions.First(), null, ActionState.Canceled, cancelReason.ToReason(), _timeProvider.GetUtcNow());
        await context.PublishAndRespond(new ActionGroupCanceled(actionGroupMessageContent, actionGroup.CorrelationId, _timeProvider.GetUtcNow().UtcDateTime));
        await _loggingService.LogOrder(_logger, LogLevel.Warning, LogCodes.OrderCanceled, actionGroup.AssignedAgv.GetValueOrDefault(),
            actionGroup.CorrelationId, "Order Canceled",
            $"Order {actionGroup.OrderId} was canceled due to {context.Message.CancelReason}",
            new Dictionary<string, object?>
            {
                { LogLabels.OrderId, actionGroup.OrderId },
                { LogLabels.Reason, context.Message.CancelReason },
                { LogLabels.ReasonDescription, context.Message.CancelReason }
            });
        if (user != null)
        {
            await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderOperatorInteraction, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Operator Interaction", "Operator interacted with order", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.OrderCanceled } });
        }

        var actionGroupWithNodes = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(actionGroup);
        if (actionGroupWithNodes.Actions.Any(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)))
        {
            var serviceNodeId = actionGroupWithNodes.Actions.First(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)).TargetNodeId!.Value;
            var serviceNode = await _agvDb.Graphs.GetNodeAsync(serviceNodeId);
            await context.Publish(new AutomaticActionWithdrawn(actionGroup.AssignedAgv, serviceNode, true));
        }

        actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="ResetActionGroup"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResetActionGroup> context)
    {
        (var correlationId, bool keepAgv) = context.Message;
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(correlationId);
        Guid? agvCorrelationId = actionGroup.AssignedAgv;
        var action = actionGroup.Actions.FirstOrDefault(a => a.State == ActionState.Started);
        if (action != null && agvCorrelationId != null)
        {
            await context.Publish(new ActionAborted(agvCorrelationId.Value, _mapper.Map<MessageAgvAction>(action)));
        }

        actionGroup.ResetActionGroup(_timeProvider.GetUtcNow().UtcDateTime, !(actionGroup.IsManual || keepAgv));
        await _actionCommandHelper.TryUpdateActionGroup(actionGroup);
        var actionGroupWithNodes = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(actionGroup);
        if (actionGroupWithNodes.Actions.Any(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)))
        {
            var serviceNodeId = actionGroupWithNodes.Actions.First(a => new[] { AgvGraph.NodeType.Charge, AgvGraph.NodeType.Park }.Contains(a.TargetNodeType)).TargetNodeId!.Value;
            var serviceNode = await _agvDb.Graphs.GetNodeAsync(serviceNodeId);
            await context.Publish(new AutomaticActionWithdrawn(actionGroup.AssignedAgv, serviceNode, true));
        }
        var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));
        await _loggingService.LogOrder(_logger, LogLevel.Warning, LogCodes.OrderReset, actionGroup.AssignedAgv.GetValueOrDefault(),
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

        _logger.LogInformation("Action group {ActionGroupId} has state {ActionGroupState} and reason {Reason} for pause", actionGroupId, actionGroup.State, string.Join(", ", actionGroup.Reasons.Select(r => r.Template)));

        if (actionGroup.State is not (ActionGroupState.Started or ActionGroupState.Paused))
        {
            _logger.LogWarning("Can not pause action group {ActionGroupId} because it is in state {ActionGroupState}", actionGroupId, actionGroup.State);
            return;
        }
        var agv = await _agvDb.Agvs.GetAgvAsync(actionGroup.AssignedAgv.GetValueOrDefault());
        var activeAction = actionGroup.FirstNotCompletedAction;
        ActionState? actionState = activeAction?.State;
        actionGroup.PauseActionGroup(pauseReason.ToReason(), _timeProvider.GetUtcNow());

        await _actionCommandHelper.TryUpdateActionGroup(actionGroup);

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
                var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
                await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));
            }
        }

        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderPaused, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroupId,
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
            _logger.LogWarning("Can not resume action group {ActionGroupId} because it is in state {ActionGroupState}", actionGroupId, actionGroup.State);
            return;
        }

        actionGroup.ResumeActionGroup(removeStationReason, _timeProvider.GetUtcNow());

        await _actionCommandHelper.TryUpdateActionGroup(actionGroup);

        var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId));

        if (actionGroup.State != ActionGroupState.Paused)
        {
            await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderResumed, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroupId,
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
        (var actionId, var actionGroupId, Guid? assignedAgv, var targetNodeId) = context.Message;
        var cancellationToken = context.CancellationToken;

        Task<GraphNode> targetNodeTask = _agvDb.Graphs.GetNodeAsync(targetNodeId);
        Task<Domain.Models.ActionGroup.ActionGroup> actionGroupTask = _actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupId);
        Task<bool> stationIsLockedTask = _actionCommandHelper.StationIsLockedIfExists(targetNodeId);

        await Task.WhenAll(targetNodeTask, actionGroupTask, stationIsLockedTask);
        var targetNode = targetNodeTask.Result;
        var actionGroup = actionGroupTask.Result;
        bool stationIsLocked = stationIsLockedTask.Result;

        var targetAction = actionGroup.Actions.First(a => a.CorrelationId == actionId);
        var previousAction = actionGroup.GetPreviousAction(actionId);

        var actionToUpdate = targetAction;
        bool shouldUpdateAction = !stationIsLocked && assignedAgv != null;

        bool canUpdatePreviousActionInstead = targetAction is { State: ActionState.Created, IsIncomplete: true } &&
                                              previousAction?.State != ActionState.Completed && previousAction?.ActionType == ActionType.Move;
        if (canUpdatePreviousActionInstead)
        {
            // Update the previous move action to directly finish the job and skip the following action
            actionToUpdate = previousAction!;
        }
        shouldUpdateAction &= actionToUpdate.State is ActionState.Started or ActionState.Failed;

        Agv? agv = null;
        Route? route = null;

        if (shouldUpdateAction)
        {
            agv = await _agvDb.Agvs.GetAgvAsync(assignedAgv!.Value);
            var agvData = await _agvDb.Data.GetAgvDataAsync(assignedAgv.Value);
            route = await _routeService.GetRouteAsync(agv, targetNodeId, null, agvData, cancellationToken);
            // Nodes will be overwritten, check before update if the start of the action was a station
            bool startNodeIsStation = await GetStartNodeIsStation(actionToUpdate);
            if (actionToUpdate.CorrelationId == targetAction.CorrelationId)
            {
                actionGroup.SetTargetForAction(actionId, targetNode, route);
            }
            else
            {
                actionToUpdate = actionGroup.SetTargetForActionWithUpdate(actionId, targetNode, route);
            }
            actionToUpdate.IncrementRevision();

            bool? previousActionGroupDockFront = null;
            if (actionToUpdate.IsFirstAction)
            {
                previousActionGroupDockFront = await _actionGroupRepository.GetDockFrontOfLastCompletedAction(agv.CorrelationId, actionGroup.CorrelationId);
            }
            DockingDirection? nodeDockingDirection = targetNode.DockingDirection;
            actionGroup.SetDockFrontOfAction(actionToUpdate.CorrelationId, previousActionGroupDockFront, nodeDockingDirection, startNodeIsStation);
            actionGroup.SetCargoLiftHeightForAction(actionToUpdate.CorrelationId, targetNode.PickLiftHeight, targetNode.DropLiftHeight);
        }

        actionGroup.SetTargetForAction(actionId, targetNode, route);

        await _actionGroupRepository.UpdateAsync(actionGroup);

        if (assignedAgv != null)
        {
            if (shouldUpdateAction)
            {
                var mappedAction = _mapper.Map<MessageAgvAction>(actionToUpdate);
                await context.Publish(new UpdateActionCommand(assignedAgv.Value, agv!.SerialNumber, mappedAction), cancellationToken);
            }
            else
            {
                if (previousAction?.State != ActionState.Started)
                {
                    await context.Publish(new AgvBecameIdle(assignedAgv.Value), cancellationToken);
                }
            }
        }

        await context.Publish(new ActionGroupChanged(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), actionGroup.CorrelationId), cancellationToken);

        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderIncomplete, actionGroup.AssignedAgv.GetValueOrDefault(),
            actionGroup.CorrelationId, "Action Target Set",
            $"Target {targetNode.Name} set for action {targetAction.ActionType} of order {actionGroup.OrderId}",
            new Dictionary<string, object?>
            {
                { LogLabels.NodeId, targetNode.Name },
                { LogLabels.ActionType, targetAction.ActionType.ToString() },
                { LogLabels.OrderId, actionGroup.OrderId }
            });
    }

    private async Task<bool> GetStartNodeIsStation(AgvAction currentAction)
    {
        if (currentAction.Nodes.Length < 1)
        {
            return false;
        }

        var nodeWithIdOnly = currentAction.Nodes.First();
        var node = await _agvDb.Graphs.GetNodeAsync(nodeWithIdOnly.Id);
        return node.IsStation();
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
        actionGroup.AbortAction(abortedActionId, abortReason != null ? [abortReason] : [], _timeProvider.GetUtcNow());

        await _actionGroupRepository.UpdateAsync(actionGroup);

        await context.Publish(new AutomaticActionWithdrawn(actionGroup.AssignedAgv, actionGroup.Actions.OrderBy(a => a.Sequence).First().Target.Node!, true));
        await context.Publish(new ActionGroupChanged(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), actionGroup.CorrelationId));

        await _loggingService.LogOrder(_logger, LogLevel.Warning, LogCodes.OrderAborted, actionGroup.AssignedAgv.GetValueOrDefault(),
            actionGroup.CorrelationId, "Order Aborted",
            $"Order {actionGroup.OrderId} was aborted due to {reason}",
            new Dictionary<string, object?>
            {
                { LogLabels.OrderId, actionGroup.OrderId },
                { LogLabels.Reason, reason },
                { LogLabels.ReasonDescription, reason }
            });
    }

    /// <summary>
    /// Handles the <see cref="TryUpdateIdleAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<TryUpdateIdleAction> context)
    {
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(context.Message.CorrelationId);
        var targetNode = actionGroup.Actions.Single(a => a.IsFirstAction).Target.Node!;

        await _actionManagerSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            // Get and select agv
            //Get a list of all action groups that are started but idling
            List<Domain.Models.ActionGroup.ActionGroup> idleActionGroups = (await _actionGroupRepository.GetRunningIdleActionGroups()).ToList();
            _logger.LogDebug("Idling action groups [{ActionGroupId}]", string.Join(',', idleActionGroups.Select(ag => ag.CorrelationId)));
            
            //Filter action groups which have assigned AGV which have other created or started pick action
            List<Guid> idleAgvCorrelationIds = idleActionGroups.Select(a => a.AssignedAgv!.Value).ToList();
            _logger.LogDebug("AGVs to check for Pick actions [{ListOfAgvs}]", string.Join(',', idleAgvCorrelationIds));
            
            List<Guid> idleAgvsMovingCargo = (await _actionGroupRepository.GetAgvsMovingCargo(idleAgvCorrelationIds)).ToList();
            if (idleAgvsMovingCargo.Count > 0)
            {
                _logger.LogInformation("AGVs with Pick or Drop actions [{ListOfAgvs}]", string.Join(',', idleAgvsMovingCargo));
                idleActionGroups = idleActionGroups.Where(ag => !idleAgvsMovingCargo.Contains(ag.AssignedAgv!.Value)).ToList();
                _logger.LogDebug("Idling action groups to consider [{ActionGroupId}]", string.Join(',', idleActionGroups.Select(ag => ag.CorrelationId)));
                // remove from idleAgvCorrelationIds as well
                idleAgvCorrelationIds = idleAgvCorrelationIds.Except(idleAgvsMovingCargo).ToList();
            }
            _logger.LogInformation("There are AGVs [{ListOfAgvs}] with started idling action groups", string.Join(',', idleAgvCorrelationIds));

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
            List<AgvData> agvsData = (await _agvDb.Data.GetSelectedAgvDataAsync(agvs.Select(a => a.CorrelationId).ToList())).OrderBy(a => a.AgvCorrelationId).ToList();

            //Create tuples of AGV and route costs to action group.
            //Skip agvs that are on the target or on the first edge to the next waypoint,
            //as it could already be moving away and running a detour to the target.
            (Guid? agv, Route? route) closestAgvRoute = await _routeService.GetAgvRouteClosestToTarget((ICollection<(Agv agv, AgvData? agvData)>) agvs.Zip(agvsData).ToList(), targetNode.Id, true);
            if (closestAgvRoute.agv == null)
            {
                _logger.LogInformation("Reactivate all parking agvs and agvs at queue heads");
                foreach (var agv in agvsAtQueueHeads.Union(readyAgvs.Select(a => a.CorrelationId)))
                {
                    await context.Publish(new AgvBecameIdle(agv));
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
            targetNode = await _agvDb.Graphs.GetNodeAsync(targetNode.Id);
            DockingDirection? nodeDockingDirection = targetNode.DockingDirection;
            actionGroup.AssignedAgv = closestAgvRoute.agv;
            bool? previousActionGroupDockFront = await _actionGroupRepository.GetDockFrontOfLastCompletedAction(closestAgvRoute.agv.Value, actionGroup.CorrelationId);
            actionGroup.StartAction(actionToStart.CorrelationId, closestAgvRoute!.route!.Nodes, closestAgvRoute.route.Edges, _timeProvider.GetUtcNow());
            actionGroup.SetDockFrontOfAction(actionToStart.CorrelationId, previousActionGroupDockFront, nodeDockingDirection, startNodeIsStation);

            // Get route costs
            var assignedAgv = agvs.SingleOrDefault(a => a.CorrelationId == actionGroup.AssignedAgv!.Value);
            if (assignedAgv == null)
            {
                _logger.LogWarning("Assigned AGV with CorrelationId {AgvCorrelationId} not found for action group {ActionGroupCorrelationId}", actionGroup.AssignedAgv, actionGroup.CorrelationId);
                return;
            }
            double duration = closestAgvRoute.route.Costs;
            duration += await _durationEstimationService.AddNonRouteCosts(false, actionGroup, actionToStart, assignedAgv);
            actionGroup.EstimatedDuration = TimeSpan.FromSeconds(duration);

            await _actionCommandHelper.TryUpdateActionGroup(actionGroup);

            // Publish messages
            await context.Publish(new AbortAndStartActionCommand(assignedAgv.CorrelationId,
                assignedAgv.SerialNumber,
                _mapper.Map<MessageAgvAction>(actionToStart), abortedAction.CorrelationId));

            await context.Publish(new ActionGroupStarted(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), actionGroup.CorrelationId, _timeProvider.GetUtcNow().UtcDateTime));
            await context.Publish(new SaveActionGroupAbortion(abortedActionGroup.CorrelationId, abortedAction.CorrelationId, abortedActionGroup.IsChargingOrder ? AbortReason.ReactivatedFromCharging : AbortReason.ReactivatedFromParking));

            // Log for ui
            await _loggingService.LogOrder(
                _logger,
                LogLevel.Information, 
                LogCodes.OrderAgvAssigned, 
                assignedAgv.CorrelationId,  
                actionGroup.CorrelationId,
                "AGV Assigned",
                $"Order {actionGroup.OrderId} was assigned to {assignedAgv.SerialNumber}",
                new Dictionary<string, object?>
                {
                    { LogLabels.OrderId, actionGroup.OrderId }, { LogLabels.AgvSerialNumber, assignedAgv.SerialNumber }
                });

            await _loggingService.LogOrder(
                _logger,
                LogLevel.Information,
                LogCodes.OrderStarted,
                actionGroup.AssignedAgv.GetValueOrDefault(),
                actionGroup.CorrelationId,
                "Order Started",
                $"Order {actionGroup.OrderId} has been started",
                new Dictionary<string, object?>
                {
                    { LogLabels.OrderId, actionGroup.OrderId }
                });
            await _loggingService.LogOrder(
                _logger,
                LogLevel.Information,
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
            await _actionCommandHelper.LogStationUpdate(assignedAgv, actionToStart, actionGroup);
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

            actionGroup.ResetActionGroup(_timeProvider.GetUtcNow());
            actionGroup.Queue();
            if (actionGroup.Priority == Priority.Default)
            {
                actionGroup.Priority = Priority.Resume;
            }

            var agvToAssign = context.Message.AssignedAgv != null ? await _agvDb.Agvs.GetAgvAsync(context.Message.AssignedAgv.Value) : null;
            if (agvToAssign == null)
            {
                ICollection<Agv> readyAgvs = await _agvDb.Agvs.GetReadyAgvsWithAtLeastLowBatteryLevel();
                readyAgvs = readyAgvs.Where(a => a.CorrelationId != assignedAgv).ToList();

                List<AgvData> agvsData = (await _agvDb.Data.GetSelectedAgvDataAsync(readyAgvs.Select(a => a.CorrelationId).ToList())).OrderBy(a => a.AgvCorrelationId).ToList();

                (Guid? agv, Route? route) closestAgvRoute = await _routeService.GetAgvRouteClosestToTarget((ICollection<(Agv agv, AgvData? agvData)>) readyAgvs.Zip(agvsData).ToList(), actionGroup.FirstNotStartedAction!.Target.NodeId!.Value);

                if (closestAgvRoute.agv == null)
                {
                    await _actionCommandHelper.TryUpdateActionGroup(actionGroup);
                    await _loggingService.LogOrder(_logger, LogLevel.Warning, LogCodes.OrderReset, actionGroup.AssignedAgv.GetValueOrDefault(),
                        actionGroup.CorrelationId, "Order Reset", $"Order {actionGroup.OrderId} was reset ",
                        new Dictionary<string, object?> { { LogLabels.OrderId, actionGroup.OrderId } });
                    await context.Publish(new ActionGroupCreated(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), actionGroup.CorrelationId, null));
                    return;
                }
                agvToAssign = readyAgvs.Single(a => a.CorrelationId == closestAgvRoute.agv!.Value);
            }

            actionGroup.Assign(agvToAssign.CorrelationId);
            actionGroup.IsManual = true;

            await _actionCommandHelper.TryUpdateActionGroup(actionGroup);

            await context.Publish(new AgvBecameIdle(agvToAssign.CorrelationId));

            await context.Publish(new ActionAborted(assignedAgv, _mapper.Map<Common.Messages.ActionGroups.AgvAction>(actionToAbort)));

            await context.Publish(new ActionGroupChanged(_mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup), actionGroup.CorrelationId));

            await context.Publish(new ActionGroupReassignedOrReset(actionGroup.CorrelationId, actionGroup.Creator));

            await _loggingService.LogOrder(
                _logger,
                LogLevel.Information,
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
        await _actionCommandHelper.TryUpdateActionGroup(actionGroup);

        var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);

        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderQueued, actionGroupMessageContent.AssignedAgv.GetValueOrDefault(),
            actionGroupMessageContent.CorrelationId, "Order Queued", $"Order {actionGroupMessageContent.OrderId} has been queued",
            new Dictionary<string, object?> { { LogLabels.OrderId, actionGroupMessageContent.OrderId } });

        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), context.CancellationToken);
        await context.Publish(new ActionGroupQueued(actionGroupMessageContent, actionGroup.CorrelationId, actionGroup.AssignedAgv), context.CancellationToken);
    }

    /// <inheritdoc cref="ActionCommandHandler"/>>
    public async Task Consume(ConsumeContext<TryRetractActionGroup> context)
    {
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(context.Message.CorrelationId);
        if (!actionGroup.IsQueued)
        {
            _logger.LogWarning("Can not retract action group {ActionGroupCorrelationId}, it is not queued", actionGroup.CorrelationId);
            return;
        }
        if (actionGroup.StartedDate != null)
        {
            _logger.LogWarning("Can not retract action group {ActionGroupCorrelationId}, it is has already been started", actionGroup.CorrelationId);
            return;
        }

        // Lock semaphore only for updating action group to not block all action group selection during retraction request
        await _actionManagerSemaphoreProvider.ExecuteLockedAsync(async () =>
        {
            // Check again if action group has been started in the meantime 
            actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(context.Message.CorrelationId);
            if (actionGroup.StartedDate != null)
            {
                _logger.LogWarning("Can not retract action group {ActionGroupCorrelationId}, it is has already been started", actionGroup.CorrelationId);
                return;
            }
            actionGroup.TryRetract();
            await _actionCommandHelper.TryUpdateActionGroup(actionGroup);
            var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
            await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), context.CancellationToken);
            await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderRetracted, actionGroupMessageContent.AssignedAgv.GetValueOrDefault(),
                actionGroupMessageContent.CorrelationId, "Order Retracted", $"Order {actionGroupMessageContent.OrderId} has been retracted",
                new Dictionary<string, object?> { { LogLabels.OrderId, actionGroupMessageContent.OrderId } });
        });
    }

    /// <inheritdoc cref="ActionCommandHandler"/>>
    public async Task Consume(ConsumeContext<HandleActionFailure> context)
    {
        var (actionGroupCorrelationId, actionCorrelationId) = context.Message;

        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupCorrelationId);
        var failedAction = actionGroup.Actions.SingleOrDefault(a => a.CorrelationId == actionCorrelationId);
        if (failedAction == null)
        {
            _logger.LogInformation("Could not handle action failure, action {ActionCorrelationId} not found in action group {ActionGroupCorrelationId}", actionCorrelationId, actionGroupCorrelationId);
            return;
        }
        bool targetIsLocked = false;
        if (failedAction.Target.Node != null)
        {
            var node = await _nodeRepository.GetNodeAsync(failedAction.Target.Node.Id);
            targetIsLocked = node.IsLocked;
        }

        string reason = actionGroup.Fail(failedAction.CorrelationId, targetIsLocked);
        await _actionCommandHelper.TryUpdateActionGroup(actionGroup);
        var actionGroupMessageContent = _mapper.Map<Common.Messages.ActionGroups.ActionGroup>(actionGroup);
        await context.Publish(new ActionGroupChanged(actionGroupMessageContent, actionGroup.CorrelationId), context.CancellationToken);
        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderFailed, actionGroupMessageContent.AssignedAgv.GetValueOrDefault(),
            actionGroupMessageContent.CorrelationId, "Order Failed", $"Order {actionGroupMessageContent.OrderId} has failed",
            new Dictionary<string, object?>
            {
                {
                    LogLabels.OrderId, actionGroupMessageContent.OrderId
                },
                {
                    LogLabels.ActionId, failedAction.CorrelationId
                },
                {
                    LogLabels.Reason, reason
                },
                {
                    LogLabels.ReasonDescription, reason
                }
            });
    }
}
