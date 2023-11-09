using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

/// <summary>
/// Execute processing of a transport order messages.
/// </summary>
[UsedImplicitly]
public class StartActionCommandConsumer :
    IConsumer<StartActionCommand>
{
    private readonly ILogger<StartActionCommandConsumer> _logger;
    private readonly IAgvRegistry _agvRegistry;

    /// <summary>
    /// Create dummy consumer with message handler delegate
    /// </summary>
    public StartActionCommandConsumer(ILogger<StartActionCommandConsumer> logger, IAgvRegistry agvRegistry)
    {
        _logger = logger;
        _agvRegistry = agvRegistry;
    }

    /// <summary>
    /// Handle message <see cref="StartActionCommand"/>. and send an order to the AGV of Type Transport, RecordType Order
    /// </summary>
    /// <param name="context">Context of the consumer wrapping details surrounding the inbound message.</param>
    /// <exception cref="AssignedAgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<StartActionCommand> context)
    {
        var action = context.Message.Action;
        var actionType = action.ActionType;
        string targetNodeId = action.TargetNodeId;
        OrderType orderType;
        string sourceToSend = string.Empty;
        string destinationToSend = string.Empty;
        string agvId = context.Message.AgvId;
        var assignedAgv = context.Message.CorrelationId;

        if (string.IsNullOrEmpty(agvId))
        {
            throw new AssignedAgvNotFoundException(assignedAgv, agvId, action);
        }

        switch (actionType)
        {
            case ActionType.Pick:
                orderType = OrderType.Pick;
                sourceToSend = action.TargetNodeId;
                break;

            case ActionType.Drop:
                orderType = OrderType.Drop;
                destinationToSend = action.TargetNodeId;
                break;

            case ActionType.Move:
                orderType = OrderType.Move;
                destinationToSend = action.TargetNodeId;
                break;

            case ActionType.Charge:
                orderType = OrderType.Charge;
                destinationToSend = action.TargetNodeId;
                break;

            default:
                throw new InvalidOperationException($"Can't handle {actionType} yet");
        }

        // We are using the ActionCorrelationId as "orderId"
        // And we transferring the Action's Guid
        await context.PublishMqtt(new MqttOrder(action.CorrelationId.ToString())
        {
            SerialNumber = agvId,
            OrderType = orderType,
            OrderId = action.CorrelationId.ToString(),
            RecordType = OrderRecordType.Order,
            Stations = new Stations
            {
                FromStationIdentifier = sourceToSend,
                ToStationIdentifier = destinationToSend
            }
        }, context.CancellationToken);

        FeedAgvRegistry(assignedAgv, action);
        _logger.LogInformation("Sent transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
            action.CorrelationId, assignedAgv, agvId, targetNodeId);
    }

    private void FeedAgvRegistry(Guid correlationId, AgvAction agvAction)
    {
        _agvRegistry.AddOrUpdateAgvAction(correlationId, agvAction);
    }
}