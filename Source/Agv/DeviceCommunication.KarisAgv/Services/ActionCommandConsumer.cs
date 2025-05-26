using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Extensions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using IAgvRepository = Gebhardt.Acc.DeviceCommunication.Common.Repositories.IAgvRepository;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Provides handlers for action-related commands.
/// </summary>
[UsedImplicitly]
public class ActionCommandConsumer(ILogger<ActionCommandConsumer> logger, IAgvRepository agvRepository, IGraphRepository graphRepository, IActionRepository actionRepository) :
    IConsumer<StartActionCommand>,
    IConsumer<UpdateActionCommand>,
    IConsumer<AbortAndStartActionCommand>,
    IConsumer<ResendAction>
{
    /// <summary>
    /// Handles the <see cref="StartActionCommand"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<StartActionCommand> context)
    {
        var action = context.Message.Action;
        string agvSerialNumber = context.Message.SerialNumber;
        var assignedAgv = context.Message.CorrelationId;

        if (string.IsNullOrEmpty(agvSerialNumber))
        {
            throw new AssignedAgvNotFoundException(assignedAgv, agvSerialNumber, action);
        }

        var order = await GetOrder(action, agvSerialNumber, OrderRecordType.Order, action.CorrelationId.ToString());

        await context.PublishMqtt(order, context.CancellationToken);

        FeedAgvRepository(assignedAgv, action);

        if (action.IsMoveToCoordinate)
        {
            logger.LogInformation("Sent transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} to coordinates: {Coordinates}",
                action.CorrelationId, assignedAgv, agvSerialNumber, action.Target.Position!);
            return;
        }
        logger.LogInformation("Sent transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
            action.CorrelationId, assignedAgv, agvSerialNumber, action.Target.NodeId);
    }

    /// <summary>
    /// Handles the <see cref="UpdateActionCommand"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateActionCommand> context)
    {
        var action = context.Message.Action;
        string agvSerialNumber = context.Message.SerialNumber;
        var assignedAgv = context.Message.CorrelationId;
        string orderId = action.CorrelationId.ToString();

        if (string.IsNullOrEmpty(agvSerialNumber))
        {
            throw new AssignedAgvNotFoundException(assignedAgv, agvSerialNumber, action);
        }

        var order = await GetOrder(action, agvSerialNumber, OrderRecordType.OrderUpdate, orderId);

        await TryRemoveOldMapping(action.CorrelationId);
        await context.PublishMqtt(order, context.CancellationToken);

        FeedAgvRepository(assignedAgv, action);

        if (action.IsMoveToCoordinate)
        {
            logger.LogInformation("Sent update of transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} to coordinates: {Coordinates}",
                action.CorrelationId, assignedAgv, agvSerialNumber, action.Target.Position!);
            return;
        }
        logger.LogInformation("Sent update transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
            action.CorrelationId, assignedAgv, agvSerialNumber, action.Target.NodeId);
    }

    private async Task TryRemoveOldMapping(Guid orderId)
    {
        Guid? internalOrderId = await actionRepository.GetInternalOrderId(orderId);
        if (internalOrderId != null)
        {
            await actionRepository.RemoveVehicleAndInternalOrderIdMapping(orderId);
        }
    }

    /// <summary>
    /// Handles the <see cref="AbortAndStartActionCommand"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AbortAndStartActionCommand> context)
    {
        var action = context.Message.Action;
        string agvSerialNumber = context.Message.SerialNumber;
        var assignedAgv = context.Message.CorrelationId;
        Guid? abortedActionCorrelationId = context.Message.AbortedActionCorrelationId;
        string orderId = abortedActionCorrelationId == null? action.CorrelationId.ToString(): abortedActionCorrelationId.Value.ToString();

        if (string.IsNullOrEmpty(agvSerialNumber))
        {
            throw new AssignedAgvNotFoundException(assignedAgv, agvSerialNumber, action);
        }

        var order = await GetOrder(action, agvSerialNumber, abortedActionCorrelationId == null ? OrderRecordType.Order : OrderRecordType.OrderUpdate, orderId);

        await context.PublishMqtt(order, context.CancellationToken);

        FeedAgvRepository(assignedAgv, action);
        if (abortedActionCorrelationId != null)
        {
            await actionRepository.AddVehicleAndInternalOrderIdMapping(abortedActionCorrelationId.Value, action.CorrelationId);
        }

        if (action.IsMoveToCoordinate)
        {
            logger.LogInformation("Sent update of transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} to coordinates: {Coordinates}",
                action.CorrelationId, assignedAgv, agvSerialNumber, action.Target.Position!);
            return;
        }
        logger.LogInformation("Aborted Action: {ActionCorrelationId} and sent update transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
            abortedActionCorrelationId, action.CorrelationId, assignedAgv, agvSerialNumber, action.Target.NodeId);
    }

    /// <summary>
    /// Handles the <see cref="ResendAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResendAction> context)
    {
        (_, string serialNumber, var action) = context.Message;

        var order = await GetOrder(action, serialNumber, OrderRecordType.Order, action.CorrelationId.ToString());
        await context.PublishMqtt(order, context.CancellationToken);

        logger.LogInformation("Resent order: {OrderId} for AGV:{AgvId} at target: {TargetNodeId}",
            action.CorrelationId, serialNumber, action.Target.NodeId);
    }

    private async Task<MqttOrder> GetOrder(AgvAction action, string agvSerialNumber, OrderRecordType recordType, string orderId)
    {
        List<object> routeNodes = await GetRouteNodes(action);
        List<object> routeEdges = await GetRouteEdges(action);

        return action.ConvertToOrder(orderId, recordType, agvSerialNumber, routeNodes, routeEdges);
    }

    private async Task<List<object>> GetRouteNodes(AgvAction action)
    {
        return (await graphRepository.GetMqttNodes([.. action.Nodes])).ToList<object>();
    }

    private async Task<List<object>> GetRouteEdges(AgvAction action)
    {
        return (await graphRepository.GetMqttEdges([.. action.Edges])).ToList<object>();
    }

    private void FeedAgvRepository(Guid correlationId, AgvAction agvAction)
    {
        agvRepository.AddOrUpdateAgvAction(correlationId, agvAction);
    }
}