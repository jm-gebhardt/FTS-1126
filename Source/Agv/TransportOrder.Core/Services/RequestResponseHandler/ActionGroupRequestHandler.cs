using AutoMapper;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

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
    IConsumer<TriggerSetTarget>
{
    private readonly IBus _bus;
    private readonly IActionGroupRetrievalService _retrievalService;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<ActionGroupRequestHandler> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    ///  Initializes a new <see cref="ActionGroupRequestHandler"/> instance.
    /// </summary>
    public ActionGroupRequestHandler(ILogger<ActionGroupRequestHandler> logger, IActionGroupRetrievalService retrievalService, IActionGroupRepository actionGroupRepository, IGraphRepository graphRepository, IBus bus, IMapper mapper)
    {
        _bus = bus;
        _retrievalService = retrievalService;
        _actionGroupRepository = actionGroupRepository;
        _graphRepository = graphRepository;
        _logger = logger;
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
        IReadOnlyCollection<ActionGroupReadModel> actionGroups = await _retrievalService.GetActionGroupsWithNodesAsync(request.States, request.FromDate, request.ToDate, request.Limit, request.AssignedAgvIds, request.SortBy, request.SortDescending);

        await context.RespondAsync(new GotActionGroups(actionGroups));
    }

    /// <summary>
    /// Handles the <see cref="GetActionGroupCountsByState"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetActionGroupCountsByState> context)
    {
        AgvGroupCountByState[] actionGroupStateCounts = await Task.WhenAll(Enum.GetValues<ActionGroupState>().Select(state => _actionGroupRepository.GetActionGroupsCountAsync(new[] { state })
            .ContinueWith(actionGroupCount => new AgvGroupCountByState(state, actionGroupCount.Result))));

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
            throw new BadRequestException("Query must be at least 3 characters.");
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

        if (actionGroup.State != ActionGroupState.Started && actionGroup.State != ActionGroupState.Paused)
        {
            throw new ActionGroupStateInvalidException(actionGroup, $"Cannot abort action groups that are in state '{actionGroup.State}'.");
        }
        IRequestClient<AbortActionGroup>? requestClient = context.CreateRequestClient<AbortActionGroup>(_bus);
        Response<Success, AbortActionGroupFailed> response = await requestClient.GetResponse<Success, AbortActionGroupFailed>(new AbortActionGroup(correlationId, context.Message.AbortReason));
        if (response.Message is AbortActionGroupFailed failed)
        {
            throw new ActionGroupStateInvalidException(actionGroup, failed.Reason);
        }
        await context.RespondAsync(new ActionGroupAbortionTriggered(correlationId));
    }

    /// <summary>
    /// Handles the <see cref="TriggerActionGroupReset"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerActionGroupReset> context)
    {
        (var correlationId, bool keepAgv) = context.Message;
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(correlationId);

        IRequestClient<ResetActionGroup>? requestClient = context.CreateRequestClient<ResetActionGroup>(_bus);
        await requestClient.GetResponse<Success>(new ResetActionGroup(actionGroup.CorrelationId, keepAgv));
        await context.RespondAsync(new ActionGroupResetTriggered(actionGroup.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="TryReassignActionGroup"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TryReassignActionGroup> context)
    {
        var correlationId = context.Message.CorrelationId;
        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(correlationId);

        if (!actionGroup.Actions.Any(a => a is { ActionType: ActionType.Pick, State: ActionState.Started or ActionState.Paused, ShouldSkip: false }))
        {
            throw new ActionGroupStateInvalidException(actionGroup, "No started or paused pick action");
        }

        await _bus.Publish(new ReassignActionGroup(actionGroup.CorrelationId, context.Message.AssignedAgv));

        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="TriggerActionCompletion"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerActionCompletion> context)
    {
        var (correlationId, actionCorrelationId) = context.Message;
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
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="TriggerActionGroupQueueing"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerActionGroupQueueing> context)
    {
        var actionGroupCorrelationId = context.Message.CorrelationId;
        await _actionGroupRepository.GetRequiredActionGroupByIdAsync(actionGroupCorrelationId);
        await _bus.Publish(new ForceQueueActionGroup(actionGroupCorrelationId));
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="TriggerSetTarget"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerSetTarget> context)
    {
        (var correlationId, var actionCorrelationId, string targetNode) = context.Message;

        var actionGroup = await _actionGroupRepository.GetRequiredActionGroupByIdAsync(correlationId);
        if (actionGroup.Actions.All(a => a.CorrelationId != actionCorrelationId))
        {
            throw new ActionNotFoundException("Action not found using id", "CorrelationId", actionCorrelationId);
        }

        await _graphRepository.ThrowIfNodeNotExistsAsync(targetNode);

        var action = actionGroup.Actions.First(a => a.CorrelationId == actionCorrelationId);
        if (action.ShouldSkip)
        {
            throw new ActionGroupStateInvalidException($"Action {action.CorrelationId} already skipped");
        }

        if (action.State == ActionState.Completed)
        {
            throw new ActionGroupStateInvalidException($"Action {action.CorrelationId} already completed");
        }

        _logger.LogInformation("Triggering setting target {TargetNode} for action {ActionCorrelationId} of action group {ActionGroupCorrelationId}", targetNode, actionCorrelationId, correlationId);
        await _bus.Publish(new SetTargetForAction(actionCorrelationId, correlationId, actionGroup.AssignedAgv, targetNode));
        await context.RespondAsync(new Success());
    }
}