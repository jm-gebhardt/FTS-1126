using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.DeviceCommunication.Common.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Extensions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Provides handlers for action-related commands.
/// </summary>
[UsedImplicitly]
public class ActionCommandConsumer(ILogger<ActionCommandConsumer> logger, IAgvRepository agvRepository, IGraphRepository graphRepository, IActionRepository actionRepository) :
    IConsumer<StartActionCommand>,
    IConsumer<UpdateActionCommand>,
    IConsumer<AbortAndStartActionCommand>

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

        var order = GetOrder(action, agvSerialNumber, OrderRecordType.Order, action.CorrelationId.ToString());

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

        var order = GetOrder(action, agvSerialNumber, OrderRecordType.OrderUpdate, orderId);

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

        var order = GetOrder(action, agvSerialNumber, abortedActionCorrelationId == null ? OrderRecordType.Order : OrderRecordType.OrderUpdate, orderId);

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
        logger.LogInformation("Sent update transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
            action.CorrelationId, assignedAgv, agvSerialNumber, action.Target.NodeId);
    }

    private MqttOrder GetOrder(AgvAction action, string agvSerialNumber, OrderRecordType recordType, string orderId)
    {
        List<object> routeNodes = GetRouteNodes(action);
        List<object> routeEdges = GetRouteEdges(action);

        return action.ConvertToOrder(orderId, recordType, agvSerialNumber, routeNodes, routeEdges);
    }

    private List<object> GetRouteNodes(AgvAction action)
    {
        List<object> routeNodes = [];
        routeNodes.AddRange(action.Nodes.Select(graphRepository.GetMqttNode).OfType<MqttNode>());
        return routeNodes;
    }

    private List<object> GetRouteEdges(AgvAction action)
    {
        List<object> routeEdges = [];
        routeEdges.AddRange(action.Edges.Select(graphRepository.GetMqttEdge).OfType<MqttEdge>());
        return routeEdges;
    }

    private void FeedAgvRepository(Guid correlationId, AgvAction agvAction)
    {
        agvRepository.AddOrUpdateAgvAction(correlationId, agvAction);
    }
}