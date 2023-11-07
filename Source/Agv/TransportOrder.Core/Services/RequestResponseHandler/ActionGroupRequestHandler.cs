using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for action group related requests.
/// </summary>
[PublicAPI]
public class ActionGroupRequestHandler : IConsumer<GetActionGroup>,
    IConsumer<GetActionGroups>,
    IConsumer<GetActionGroupCountsByState>,
    IConsumer<TriggerActionGroupAbortion>
{
    private readonly IActionGroupRetrievalService _retrievalService;
    private readonly IActionGroupRegistry _actionGroupRegistry;
    private readonly IBus _bus;

    /// <summary>
    ///  Initializes a new <see cref="ActionGroupRequestHandler"/> instance.
    /// </summary>
    /// <param name="retrievalService">The action group read model retrieval service.</param>
    /// <param name="actionGroupRegistry">The action group repository.</param>
    /// <param name="bus">The message bus.</param>
    public ActionGroupRequestHandler(IActionGroupRetrievalService retrievalService, IActionGroupRegistry actionGroupRegistry, IBus bus)
    {
        _retrievalService = retrievalService;
        _actionGroupRegistry = actionGroupRegistry;
        _bus = bus;
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
        AgvGroupCountByState[] actionGroupStateCounts = await Task.WhenAll(Enum.GetValues<ActionGroupState>().Select(state =>
            _actionGroupRegistry.GetActionGroupsCountAsync(new[] {state})
                .ContinueWith(actionGroupCount => new AgvGroupCountByState(state, actionGroupCount.Result))));

        await context.RespondAsync(new GotActionGroupCountsByState(actionGroupStateCounts));
    }

    /// <summary>
    /// Handles the <see cref="TriggerActionGroupAbortion"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerActionGroupAbortion> context)
    {
        var correlationId = context.Message.CorrelationId;
        var actionGroup = await _actionGroupRegistry.GetRequiredActionGroupByIdAsync(correlationId);

        if (actionGroup.State != ActionGroupState.Started)
        {
            throw new ActionGroupStateInvalidException(actionGroup, "Cannot abort action that are not in state 'Started'.");
        }
        IRequestClient<AbortActionGroup>? requestClient = context.CreateRequestClient<AbortActionGroup>(_bus);
        Response<Success, AbortActionGroupFailed> response = await requestClient.GetResponse<Success, AbortActionGroupFailed>( new AbortActionGroup(correlationId));
        if (response.Message is AbortActionGroupFailed failed)
        {
            throw new ActionGroupStateInvalidException(actionGroup, failed.Reason);
        }
        await context.RespondAsync(new ActionGroupAbortionTriggered(correlationId));
    }
}