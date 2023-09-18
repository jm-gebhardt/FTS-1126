using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using JetBrains.Annotations;
using MassTransit;
using TransportOrder.Domain.Exceptions;
using TransportOrder.Domain.ReadModels;
using TransportOrder.Domain.Repositories;
using TransportOrder.Messages.Requests.ActionGroup;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for action group related requests.
/// </summary>
[PublicAPI]
public class ActionGroupRequestHandler : IConsumer<GetActionGroup>,
    IConsumer<GetActionGroups>,
    IConsumer<GetActionGroupCountsByState>
{
    private readonly IActionGroupRetrievalService _retrievalService;
    private readonly IActionGroupRegistry _actionGroupRegistry;

    /// <summary>
    ///  Initializes a new <see cref="ActionGroupRequestHandler"/> instance.
    /// </summary>
    /// <param name="retrievalService">The action group read model retrieval service.</param>
    /// <param name="actionGroupRegistry">The action group repository.</param>
    public ActionGroupRequestHandler(IActionGroupRetrievalService retrievalService, IActionGroupRegistry actionGroupRegistry)
    {
        _retrievalService = retrievalService;
        _actionGroupRegistry = actionGroupRegistry;
    }

    /// <summary>
    /// Handles the  <see cref="GetActionGroup"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetActionGroup> context)
    {
        var correlationId = context.Message.CorrelationId;
        var actionGroup = await _retrievalService.GetActionGroupWithTargetNodeTypesAsync(correlationId);
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
        IReadOnlyCollection<ActionGroupReadModel> actionGroups = await _retrievalService.GetActionGroupsWithTargetNodeTypeAsync(request.States, request.FromDate, request.ToDate, request.Limit);

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
}