using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.WCSCommunication.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Services.EventHandler;

/// <summary>
/// Provides handlers for action group related events.
/// </summary>
[UsedImplicitly]
public class ActionGroupEventHandler:
    IConsumer<ActionGroupInvalid>,
    IConsumer<ActionGroupStarted>,
    IConsumer<ActionGroupCompleted>,
    IConsumer<ActionGroupCancelled>,
    IConsumer<ActionGroupCancelFailed>,
    IConsumer<ActionGroupFailed>
{
    /// <summary>
    /// Handles the <see cref="ActionGroupInvalid"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupInvalid> context)
    {
        await context.Publish(new RemoveInvalidHostOrder(context.Message.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupStarted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupStarted> context)
    {
        var (actionGroup, _, timestamp) = context.Message;
        await context.Publish(new HandleHostOrderStart(actionGroup.CorrelationId, actionGroup.AssignedAgv!.Value.ToString(), timestamp));
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupCompleted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCompleted> context)
    {
        var (_, correlationId, timestamp) = context.Message;
        await context.Publish(new FinishHostOrder(correlationId, timestamp));
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupCancelled"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCancelled> context)
    {
        var (_, correlationId, timestamp) = context.Message;
        await context.Publish(new FinishHostOrderCancellation(correlationId, timestamp));
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupCancelFailed"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCancelFailed> context)
    {
        var ( _,  correlationId,  errorCode) = context.Message;
        await context.Publish(new HandleCancellationFailure(correlationId, errorCode));
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupFailed"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupFailed> context)
    {
        var (_, correlationId, errorCode, timestamp) = context.Message;
        await context.Publish(new HandleHostOrderFailure(correlationId, errorCode, timestamp));
    }
}