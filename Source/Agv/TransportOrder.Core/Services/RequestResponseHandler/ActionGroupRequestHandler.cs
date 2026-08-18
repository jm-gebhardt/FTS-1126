using AutoMapper;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Filter.Api;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using ActionGroup = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.ActionGroup;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for action group related requests.
/// </summary>
[PublicAPI]
public class ActionGroupRequestHandler : IConsumer<GetActionGroup>,
    IConsumer<GetActionGroups>,
    IConsumer<GetActionGroupCountsByState>,
    IConsumer<GetHandlingUnits>,
    IConsumer<TriggerActionGroupAbortion>,
    IConsumer<TriggerActionGroupReset>,
    IConsumer<TryReassignActionGroup>,
    IConsumer<TriggerActionCompletion>,
    IConsumer<TriggerActionGroupQueueing>,
    IConsumer<TriggerSetTarget>,
    IConsumer<GetFilteredActionGroups>
{
    private readonly IBus _bus;
    private readonly IActionGroupRetrievalService _retrievalService;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<ActionGroupRequestHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ILoggingService _loggingService;

    /// <summary>
    ///  Initializes a new <see cref="ActionGroupRequestHandler"/> instance.
    /// </summary>
    public ActionGroupRequestHandler(ILogger<ActionGroupRequestHandler> logger, IActionGroupRetrievalService retrievalService, IActionGroupRepository actionGroupRepository, IGraphRepository graphRepository, ILoggingService loggingService, IBus bus, IMapper mapper)
    {
        _bus = bus;
        _retrievalService = retrievalService;
        _actionGroupRepository = actionGroupRepository;
        _graphRepository = graphRepository;
        _logger = logger;
        _loggingService = loggingService;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the  <see cref="GetActionGroup"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetActionGroup> context)
    {
        var correlationId = context.Message.CorrelationId;

        var actionGroup = await _retrievalService.GetActionGroupWithNodesAsync(correlationId);
        if (actionGroup == null)
        {
            throw new ActionGroupNotFoundException(correlationId);
        }
        await context.RespondAsync(new GotAgvActionGroup(actionGroup));
    }

    /// <summary>
    /// Handles the <see cref="GetActionGroups"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetActionGroups> context)
    {
        var request = context.Message;
        var cancellationToken = context.CancellationToken;

        IReadOnlyCollection<ActionGroupReadModel> actionGroups = await _retrievalService.GetActionGroupsWithNodesAsync(request.States, request.FromDate, request.ToDate, request.Limit, request.AssignedAgvIds, request.SortBy, request.SortDescending, cancellationToken);

        await context.RespondAsync(new GotActionGroups(actionGroups));
    }

    /// <summary>
    /// Handles the <see cref="GetActionGroupCountsByState"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetActionGroupCountsByState> context)
    {
        var cancellationToken = context.CancellationToken;
        AgvGroupCountByState[] actionGroupStateCounts = await Task.WhenAll(Enum.GetValues<ActionGroupState>().Select(state => _actionGroupRepository.GetActionGroupsCountAsync(new[] { state }, cancellationToken: cancellationToken)
            .ContinueWith(actionGroupCount => new AgvGroupCountByState(state, actionGroupCount.Result), cancellationToken)));

        await context.RespondAsync(new GotActionGroupCountsByState(actionGroupStateCounts));
    }

    /// <summary>
    /// Handles the <see cref="GetHandlingUnits"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetHandlingUnits> context)
    {
        string query = context.Message.Query.Trim();
        if (query.Length < 3)
        {
            throw new ValidationException("Query must be at least 3 characters.");
        }

        int maxItems = Math.Clamp(context.Message.Limit, 1, 100);

        List<string> hus = await _actionGroupRepository.GetHandlingUnitsAsync(query, maxItems);
        await context.RespondAsync(new GotHandlingUnits(hus));
    }

    /// <summary>
    /// Handles the <see cref="TriggerActionGroupAbortion"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerActionGroupAbortion> context)
    {
        var correlationId = context.Message.CorrelationId;
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(correlationId);

        if (actionGroup.State != ActionGroupState.Started && actionGroup.State != ActionGroupState.Paused && actionGroup.State != ActionGroupState.Failed)
        {
            throw new ActionGroupStateInvalidException(actionGroup, $"Cannot abort action groups that are in state '{actionGroup.State}'.");
        }
        IRequestClient<AbortActionGroup>? requestClient = context.CreateRequestClient<AbortActionGroup>(_bus);
        Response<Success, AbortActionGroupFailed> response = await requestClient.GetResponse<Success, AbortActionGroupFailed>(new AbortActionGroup(correlationId, context.Message.AbortReason));
        if (response.Message is AbortActionGroupFailed failed)
        {
            throw new ActionGroupStateInvalidException(actionGroup, failed.Reason);
        }
        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderOperatorInteraction, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Operator Interaction", "Operator interacted with order", new Dictionary<string, object?> { { LogLabels.User, context.Message.User }, { LogLabels.Operation, LogCodes.OrderAborted } });
        await context.RespondAsync(new ActionGroupAbortionTriggered(correlationId));
    }

    /// <summary>
    /// Handles the <see cref="TriggerActionGroupReset"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerActionGroupReset> context)
    {
        (var correlationId, bool keepAgv, string user) = context.Message;
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(correlationId);

        IRequestClient<ResetActionGroup>? requestClient = context.CreateRequestClient<ResetActionGroup>(_bus);
        await requestClient.GetResponse<Success>(new ResetActionGroup(actionGroup.CorrelationId, keepAgv));
        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderOperatorInteraction, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Operator Interaction", "Operator interacted with order", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.OrderReset } });
        await context.RespondAsync(new ActionGroupResetTriggered(actionGroup.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="TryReassignActionGroup"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TryReassignActionGroup> context)
    {
        var correlationId = context.Message.CorrelationId;
        _logger.LogInformation("Trying to reassign action group {ActionGroupCorrelationId} to agv {AssignedAgvCorrelationId}", correlationId, context.Message.AssignedAgv);
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(correlationId);

        if (!actionGroup.Actions.Any(a => a is { ActionType: ActionType.Pick, State: ActionState.Started or ActionState.Paused, ShouldSkip: false }))
        {
            throw new ActionGroupStateInvalidException(actionGroup, "No started or paused pick action");
        }

        await _bus.Publish(new ReassignActionGroup(actionGroup.CorrelationId, context.Message.AssignedAgv));
        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderOperatorInteraction, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Operator Interaction", "Operator interacted with order", new Dictionary<string, object?> { { LogLabels.User, context.Message.User }, { LogLabels.Operation, LogCodes.OrderReassigned } });

        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="TriggerActionCompletion"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerActionCompletion> context)
    {
        (var correlationId, var actionCorrelationId, string user) = context.Message;
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(correlationId);
        if (actionGroup.Actions.All(a => a.CorrelationId != actionCorrelationId))
        {
            throw new ActionNotFoundException("Action not found using id", "CorrelationId", actionCorrelationId);
        }

        _logger.LogInformation("Triggering completion of action {ActionCorrelationId} of action group {ActionGroupCorrelationId}", actionCorrelationId, correlationId);
        if (actionGroup.AssignedAgv != null)
        {
            _logger.LogInformation("Sending abort action {ActionCorrelationId} to agv {AgvCorrelationId}, as this action was just completed", actionCorrelationId, actionGroup.AssignedAgv.Value);
            await _bus.Publish(new ActionAborted(actionGroup.AssignedAgv.Value, _mapper.Map<AgvAction>(actionGroup.Actions.Single(a => a.CorrelationId == actionCorrelationId)), true));
        }

        await _bus.Publish(new CompleteActionOfActionGroup(actionCorrelationId, correlationId, ActionGroupCreator.TransportOrderWebApi));
        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderOperatorInteraction, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Operator Interaction", "Operator interacted with order", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.OrderActionCompleted } });

        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="TriggerActionGroupQueueing"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerActionGroupQueueing> context)
    {
        var actionGroupCorrelationId = context.Message.CorrelationId;
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupCorrelationId);
        await _bus.Publish(new ForceQueueActionGroup(actionGroupCorrelationId));
        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderOperatorInteraction, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Operator Interaction", "Operator interacted with order", new Dictionary<string, object?> { { LogLabels.User, context.Message.User }, { LogLabels.Operation, LogCodes.OrderQueued } });

        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="TriggerSetTarget"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerSetTarget> context)
    {
        (var correlationId, var actionCorrelationId, var targetNodeId, string user) = context.Message;

        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(correlationId);
        if (actionGroup.Actions.All(a => a.CorrelationId != actionCorrelationId))
        {
            throw new ActionNotFoundException("Action not found using id", "CorrelationId", actionCorrelationId);
        }

        var action = actionGroup.Actions.First(a => a.CorrelationId == actionCorrelationId);
        if (action.ShouldSkip)
        {
            throw new ActionGroupStateInvalidException($"Action {action.CorrelationId} already skipped");
        }

        if (action.State == ActionState.Completed)
        {
            throw new ActionGroupStateInvalidException($"Action {action.CorrelationId} already completed");
        }

        GraphNode? targetNode;
        try
        {
            targetNode = await _graphRepository.GetNodeAsync(targetNodeId);
        }
        catch (NodeNotFoundException ex)
        {
            throw new ValidationException(ex.Message);
        }

        if (action.ActionType == ActionType.Pick && !targetNode.IsPickStation())
        {
            throw new InvalidNodeTypeException(targetNode.Name, targetNode.Type.ToString(), $"{nameof(AgvGraph.NodeType.Pick)}");
        }
        if (action.ActionType == ActionType.Drop && !targetNode.IsDropStation())
        {
            throw new InvalidNodeTypeException(targetNode.Name, targetNode.Type.ToString(), $"{nameof(AgvGraph.NodeType.Drop)}");
        }

        _logger.LogInformation("Triggering setting target {TargetNode} for action {ActionCorrelationId} of action group {ActionGroupCorrelationId}", targetNode.Name, actionCorrelationId, correlationId);
        await _bus.Publish(new SetTargetForAction(actionCorrelationId, correlationId, actionGroup.AssignedAgv, targetNode.Id));
        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderOperatorInteraction, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Operator Interaction", "Operator interacted with order", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.OrderTargetSet } });
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="GetFilteredActionGroups"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetFilteredActionGroups> context)
    {
        var cancellationToken = context.CancellationToken;
        (var filterDto, int pageNumber, int pageSize, var ordering, ICollection<string> orderProps) = context.Message;

        var filter = filterDto.Translate<ActionGroup, ActionGroupFilterable>();
        
        PagedResult<FilteredActionGroup> result = await _retrievalService.Filter(filter, pageNumber, pageSize, ordering, orderProps, cancellationToken);

        await context.RespondAsync(new GotFilteredActionGroups(result));
    }
}