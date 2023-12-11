using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
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
    IConsumer<ActionGroupCanceled>,
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
        if (actionGroup.Creator is ActionGroupCreator.Wms or ActionGroupCreator.Simulation)
        {
            await context.Publish(new HandleHostOrderStart(actionGroup.CorrelationId, actionGroup.AssignedAgv!.Value.ToString(), timestamp));
        }
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupCompleted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCompleted> context)
    {
        var (actionGroup, correlationId, timestamp) = context.Message;
        if (actionGroup.Creator is ActionGroupCreator.Wms or ActionGroupCreator.Simulation)
        {
            await context.Publish(new FinishHostOrder(correlationId, timestamp));
        }
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupCanceled"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCanceled> context)
    {
        var (actionGroup, correlationId, timestamp) = context.Message;
        if (actionGroup.Creator is ActionGroupCreator.Wms or ActionGroupCreator.Simulation)
        {
            await context.Publish(new FinishHostOrderCancellation(correlationId, timestamp));
        }
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupCancelFailed"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCancelFailed> context)
    {
        var (actionGroup,  correlationId,  errorCode) = context.Message;
        if (actionGroup.Creator is ActionGroupCreator.Wms or ActionGroupCreator.Simulation)
        {
            await context.Publish(new HandleCancellationFailure(correlationId, errorCode));
        }
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupFailed"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupFailed> context)
    {
        var (actionGroup, correlationId, errorCode, timestamp) = context.Message;
        if (actionGroup.Creator is ActionGroupCreator.Wms or ActionGroupCreator.Simulation)
        {
            await context.Publish(new HandleHostOrderFailure(correlationId, errorCode, timestamp));
        }
    }
}