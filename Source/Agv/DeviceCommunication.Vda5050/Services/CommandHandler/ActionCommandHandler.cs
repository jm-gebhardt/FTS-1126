using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Order;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Order;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.CommandHandler;

/// <summary>
/// Provides handlers for action-related commands.
/// </summary>
[UsedImplicitly]
public class ActionCommandHandler(ILogger<ActionCommandHandler> logger, IOrderBuilder orderBuilder, IAgvRepository agvRepository) :
    IConsumer<StartActionCommand>,
    IConsumer<UpdateActionCommand>,
    IConsumer<ResendAction>,
    IConsumer<AbortAndStartActionCommand>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<StartActionCommand> context)
    {
        var action = context.Message.Action;
        var agvCorrelationId = context.Message.CorrelationId;

        logger.LogInformation("Received {Message} for agv {agvCorrelationId}, order {ActionCorrelationId}", nameof(StartActionCommand), agvCorrelationId, action.CorrelationId);

        var agv = await agvRepository.GetAgvAsync(agvCorrelationId);

        if (agv == null)
        {
            logger.LogDebug("No Agv found for starting action for AGV {AgvCorrelationId}, skipping", agvCorrelationId);
            return;
        }
        
        var order = await orderBuilder.Build(agvCorrelationId, action);
        
        agv.CurrentOrder = action.CorrelationId;
        agv.CurrentFleetManagerActionId = action.CorrelationId;
        await agvRepository.AddOrUpdateAsync(agv);

        logger.LogInformation("Sent transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
            action.CorrelationId, agv.CurrentOrder, agv.SerialNumber, action.Target.Node?.Name);
        await context.Publish(new StartOrder(action.CorrelationId, order));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<UpdateActionCommand> context)
    {
        var action = context.Message.Action;
        var agvCorrelationId = context.Message.CorrelationId;
        logger.LogInformation("Received {Message} for agv {agvCorrelationId}, updated order {ActionCorrelationId}", nameof(UpdateActionCommand), agvCorrelationId, action.CorrelationId);

        var agv = await agvRepository.GetAgvAsync(agvCorrelationId);

        if (agv == null)
        {
            logger.LogDebug("No Agv found for updating action for AGV {AgvCorrelationId}, skipping", agvCorrelationId);
            return;
        }
        
        var order = await orderBuilder.Build(agvCorrelationId, action);

        if (agv.CurrentOrder == null)
        {
            agv.CurrentOrder = action.CorrelationId;
            await agvRepository.AddOrUpdateAsync(agv);

            logger.LogInformation("Sent transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
                action.CorrelationId, agv.CurrentOrder, agv.SerialNumber, action.Target.Node?.Name);
            await context.Publish(new StartOrder(action.CorrelationId, order));
            return;
        }

        if (agv.CurrentOrder != action.CorrelationId)
        {
            await context.Publish(new ActionFailedByAgv(action.CorrelationId, agv.CorrelationId));
            var logEntry = new LogEntryDto
            {
                ActionGroupCorrelationId = null,
                AgvCorrelationId = agv.CorrelationId,
                Category = Categories.Order,
                LogCode = LogCodes.AgvSentActionFailedForOrder,
                LogLevel = nameof(LogLevel.Information),
                Id = Guid.Empty,
                Variables = new Dictionary<string, string>
                {
                    { LogLabels.OriginalActionId, agv.CurrentOrder!.Value.ToString() },
                    { LogLabels.ActionId, action.CorrelationId.ToString() },
                    { LogLabels.Reason, "Agv currently executes different order" }
                }
            };

            await context.Publish(new LogOccurred(logEntry, [Categories.Order]));
            return;
        }
        logger.LogInformation("Sent update of transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
            action.CorrelationId, agv.CurrentOrder, agv.SerialNumber, action.Target.Node?.Name);
        await context.Publish(new OrderUpdateRequested(action.CorrelationId, order, action.CorrelationId));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<ResendAction> context)
    {
        (var agvCorrelationId, string serialNumber, var action) = context.Message;
        logger.LogInformation("Resent order: {OrderId} for AGV:{AgvId} at target: {TargetNodeId}", action.CorrelationId, agvCorrelationId, action.Target.Node?.Name);

        var agv = await agvRepository.GetAgvAsync(agvCorrelationId);

        if (agv == null)
        {
            logger.LogDebug("No Agv found for resending action for AGV {AgvCorrelationId}, skipping", agvCorrelationId);
            return;
        }
        if (agv.CurrentOrder != action.CorrelationId)
        {
            logger.LogWarning("Action requested to resend {MessageActionCorrelationId} not current action {CurrentActionCorrelationId} of agv {AgvCorrelationId}/{AgvSerialNumber}, sending current action", action.CorrelationId, agv.CorrelationId, serialNumber, agv.CurrentOrder);
        }
        await context.Publish(new OrderResendRequested(agv.CurrentOrder ?? action.CorrelationId));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AbortAndStartActionCommand> context)
    {
        (var agvCorrelationId, string serialNumber, var action, Guid? abortedActionCorrelationId) = context.Message;

        var agv = await agvRepository.GetAgvAsync(agvCorrelationId);

        if (agv == null)
        {
            logger.LogDebug("No Agv found for abort and start action for AGV {AgvCorrelationId}, skipping", agvCorrelationId);
            return;
        }
        var order = await orderBuilder.Build(agvCorrelationId, action);

        logger.LogInformation("Received {Message} for agv {AgvCorrelationId}/{AgvSerialNumber}, aborted order {AbortedActionCorrelationId}, updated order {ActionCorrelationId}", nameof(AbortAndStartActionCommand), agvCorrelationId, serialNumber, abortedActionCorrelationId, action.CorrelationId);
        if (abortedActionCorrelationId == null && agv.CurrentOrder == null)
        {
            agv.CurrentOrder = action.CorrelationId;
            await agvRepository.AddOrUpdateAsync(agv);

            logger.LogInformation("Sent transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
                action.CorrelationId, agv.CurrentOrder, agv.SerialNumber, action.Target.Node?.Name);
            await context.Publish(new StartOrder(action.CorrelationId, order));
            return;
        }

        order.OrderId = agv.CurrentOrder!.Value.ToString();
        agv.CurrentFleetManagerActionId = abortedActionCorrelationId;
        await agvRepository.AddOrUpdateAsync(agv);
        logger.LogInformation("Sent update of transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
            action.CorrelationId, agv.CurrentOrder, agv.SerialNumber, action.Target.Node?.Name);
        await context.Publish(new OrderUpdateRequested(abortedActionCorrelationId!.Value, order, action.CorrelationId ));
    }
}