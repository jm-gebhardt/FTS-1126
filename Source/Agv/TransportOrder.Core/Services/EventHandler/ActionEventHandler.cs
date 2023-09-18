using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Provides a handler for action group related events.
/// </summary>
[UsedImplicitly]
public class ActionEventHandler : IConsumer<ActionGroupCreatedForAgv>,
    IConsumer<QueueActionGroup>,
    IConsumer<ActionCompletedEvent>
{
    private readonly ILogger<AgvEventHandler> _logger;
    private readonly IActionManager _actionManager;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="AgvEventHandler"/> instance.
    /// </summary>
    public ActionEventHandler(
        IActionManager actionManager,
        IMapper mapper,
        ILogger<AgvEventHandler> logger)
    {
        _logger = logger;
        _actionManager = actionManager;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupCreatedForAgv"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCreatedForAgv> context)
    {
        var (actionGroup, assignedAgv) = context.Message;

        var action = actionGroup.Actions.OrderBy(a => a.Sequence).First();

        await context.Publish(new StartAction(action, assignedAgv));
    }

    /// <summary>
    /// Handle <see cref="ActionGroup"/> incoming from HostOrderStateMachine
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<QueueActionGroup> context)
    {
        _logger.LogInformation("{MessageType} {CorrelationId} received. The ActionGroup is Manual = {IsManual}", nameof(QueueActionGroup), context.Message.CorrelationId, context.Message.ActionGroup.IsManual);
        await _actionManager.AddActionGroupAsync(_mapper.Map<Domain.Models.ActionGroup.ActionGroup>(context.Message.ActionGroup), context.CancellationToken);
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handle <see cref="ActionCompletedEvent"/> from DeviceCommunication
    /// </summary>
    public async Task Consume(ConsumeContext<ActionCompletedEvent> context)
    {
        var actionCorrelationId = context.Message.CorrelationId;
        _logger.LogDebug("{MessageType} for actionId: {ActionCorrelationId} received", nameof(ActionCompletedEvent), actionCorrelationId);
        await context.Publish(new CompleteAction(actionCorrelationId));
    }
}