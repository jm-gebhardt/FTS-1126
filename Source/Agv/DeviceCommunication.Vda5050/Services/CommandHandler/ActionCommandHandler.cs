using Gebhardt.Acc.Common.Messages.ActionGroups.Dispatch;
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
    IConsumer<DispatchRouteCommand>,
    IConsumer<UpdateDispatchedRouteCommand>,
    IConsumer<ResendDispatchedRouteCommand>,
    IConsumer<AbortAndDispatchRouteCommand>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<DispatchRouteCommand> context)
    {
        var action = context.Message.Action;
        var agvCorrelationId = context.Message.CorrelationId;

        logger.LogInformation("Received {Message} for agv {agvCorrelationId}, order {ActionCorrelationId}", nameof(DispatchRouteCommand), agvCorrelationId, action.CorrelationId);

        var agv = await agvRepository.GetAgvAsync(agvCorrelationId);

        if (agv == null)
        {
            logger.LogDebug("No Agv found for starting action for AGV {AgvCorrelationId}, skipping", agvCorrelationId);
            return;
        }

        var order = await orderBuilder.Build(agvCorrelationId, context.Message.Route, action);
        
        agv.CurrentOrder = action.CorrelationId;
        agv.CurrentFleetManagerActionId = action.CorrelationId;
        await agvRepository.AddOrUpdateAsync(agv, context.CancellationToken);

        logger.LogInformation("Sent transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
            action.CorrelationId, agv.CurrentOrder, agv.SerialNumber, action.Target.Node?.Name);
        await context.Publish(new StartOrder(action.CorrelationId, order));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<UpdateDispatchedRouteCommand> context)
    {
        var action = context.Message.Action;
        var agvCorrelationId = context.Message.CorrelationId;
        logger.LogInformation("Received {Message} for agv {agvCorrelationId}, updated order {ActionCorrelationId}", nameof(UpdateDispatchedRouteCommand), agvCorrelationId, action.CorrelationId);

        var agv = await agvRepository.GetAgvAsync(agvCorrelationId);

        if (agv == null)
        {
            logger.LogDebug("No Agv found for updating action for AGV {AgvCorrelationId}, skipping", agvCorrelationId);
            return;
        }

        var order = await orderBuilder.Build(agvCorrelationId, context.Message.Route, action);

        if (agv.CurrentOrder == null)
        {
            agv.CurrentOrder = action.CorrelationId;
            agv.CurrentFleetManagerActionId = action.CorrelationId;
            await agvRepository.AddOrUpdateAsync(agv, context.CancellationToken);

            logger.LogInformation("Sent transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
                action.CorrelationId, agv.CurrentOrder, agv.SerialNumber, action.Target.Node?.Name);
            await context.Publish(new StartOrder(action.CorrelationId, order));
            return;
        }

        order.OrderId = agv.CurrentOrder!.Value.ToString();
        agv.CurrentFleetManagerActionId = action.CorrelationId;
        await agvRepository.AddOrUpdateAsync(agv, context.CancellationToken);

        logger.LogInformation("Sent update of transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
            action.CorrelationId, agv.CurrentOrder, agv.SerialNumber, action.Target.Node?.Name);
        await context.Publish(new OrderUpdateRequested(agv.CurrentOrder.Value, order, action.CorrelationId));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<ResendDispatchedRouteCommand> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        string serialNumber = context.Message.SerialNumber;
        var action = context.Message.Action;
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
    public async Task Consume(ConsumeContext<AbortAndDispatchRouteCommand> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        string serialNumber = context.Message.SerialNumber;
        var action = context.Message.Action;
        Guid? abortedActionCorrelationId = context.Message.AbortedActionCorrelationId;

        var agv = await agvRepository.GetAgvAsync(agvCorrelationId);

        if (agv == null)
        {
            logger.LogDebug("No Agv found for abort and start action for AGV {AgvCorrelationId}, skipping", agvCorrelationId);
            return;
        }

        var order = await orderBuilder.Build(agvCorrelationId, context.Message.Route, action);

        logger.LogInformation("Received {Message} for agv {AgvCorrelationId}/{AgvSerialNumber}, aborted order {AbortedActionCorrelationId}, updated order {ActionCorrelationId}", nameof(AbortAndDispatchRouteCommand), agvCorrelationId, serialNumber, abortedActionCorrelationId, action.CorrelationId);
        if (abortedActionCorrelationId == null && agv.CurrentOrder == null)
        {
            agv.CurrentOrder = action.CorrelationId;
            await agvRepository.AddOrUpdateAsync(agv, context.CancellationToken);

            logger.LogInformation("Sent transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
                action.CorrelationId, agv.CurrentOrder, agv.SerialNumber, action.Target.Node?.Name);
            await context.Publish(new StartOrder(action.CorrelationId, order));
            return;
        }

        order.OrderId = agv.CurrentOrder!.Value.ToString();
        agv.CurrentFleetManagerActionId = action.CorrelationId;
        await agvRepository.AddOrUpdateAsync(agv, context.CancellationToken);
        logger.LogInformation("Sent update of transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
            action.CorrelationId, agv.CurrentOrder, agv.SerialNumber, action.Target.Node?.Name);
        await context.Publish(new OrderUpdateRequested(abortedActionCorrelationId!.Value, order, action.CorrelationId ));
    }
}