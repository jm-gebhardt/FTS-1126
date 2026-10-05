using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Services.EventHandler;

/// <summary>
/// Provides handlers for action group related events.
/// </summary>
[UsedImplicitly]
public class ActionGroupEventHandler(IHostOrderRepository hostOrderRepository, ILogger<ActionGroupEventHandler> logger) :
    IConsumer<ActionGroupCompleted>,
    IConsumer<ActionGroupStarted>,
    IConsumer<ActionGroupInvalid>,
    IConsumer<PickActionCompleted>,
    IConsumer<ActionAborted>,
    IConsumer<ActionGroupCanceled>,
    IConsumer<HostOrderValidationFailed>
{
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
    /// Handles the <see cref="ActionGroupStarted"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupStarted> context)
    {
        var (actionGroup, _, timestamp) = context.Message;
        if (actionGroup.Creator is ActionGroupCreator.Wms or ActionGroupCreator.Simulation)
        {
            await context.Publish(new HandleHostOrderStart(actionGroup.CorrelationId, actionGroup.AssignedAgv!.Value, timestamp));
        }
    }

    /// <summary>
    /// Handles the <see cref="PickActionCompleted"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<PickActionCompleted> context)
    {
        var (correlationId, actionCorrelationId, timestamp) = context.Message;
        try
        {
            await hostOrderRepository.GetByIdAsync(correlationId);
            await context.Publish(new FinishPick(correlationId, actionCorrelationId, timestamp));
        }
        catch (InvalidOperationException)
        {
            logger.LogDebug("Order {CorrelationId} not created by prismat", correlationId);
        }
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupInvalid"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupInvalid> context)
    {
        var (correlationId, _, validationErrors) = context.Message;

        if (validationErrors.Contains(HostOrderFailureReasons.ThereMustBeAtLeastOneAgvThatKnowsTheCompleteRouteOfAllActionsWithinTheGivenActionGroup))
        {
            await context.Publish(new CancelInvalidHostOrder(correlationId));
        }
    }

    /// <summary>
    /// Handles the <see cref="ActionAborted"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionAborted> context)
    {
        (_, var action, bool isTemporary) = context.Message;

        if (isTemporary)
        {
            return;
        }
        await context.Publish(new AbortHostOrder(action.ParentId));
    }

    /// <summary>
    /// Handles the <see cref="ActionGroupCanceled"/> message.
    /// </summary>
    public async Task Consume(ConsumeContext<ActionGroupCanceled> context)
    {
        var (_, correlationId, _) = context.Message;
      
        try
        {
            await hostOrderRepository.GetByIdAsync(correlationId);
            logger.LogInformation("Action group cancelled {ActionGroupCorrelationId}", correlationId);
            await context.Publish(new AbortHostOrder(correlationId));
        }
        catch (InvalidOperationException)
        {
            logger.LogDebug("Order {CorrelationId} not created by prismat", correlationId);
        }
    }

    /// <inheritdoc/>>
    public async Task Consume(ConsumeContext<HostOrderValidationFailed> context)
    {
        await context.Publish(new InvalidateHosterOrder(context.Message.CorrelationId, context.Message.Error));
    }
}
