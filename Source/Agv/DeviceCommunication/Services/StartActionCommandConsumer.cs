using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.DeviceCommunication.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.Messages.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Services;

/// <summary>
/// Execute processing of a transport order messages.
/// </summary>
[UsedImplicitly]
public class StartActionCommandConsumer :
    IConsumer<StartActionCommand>
{
    private readonly ILogger<StartActionCommandConsumer> _logger;
    private readonly IAgvRepository _agvRepository;

    /// <summary>
    /// Create dummy consumer with message handler delegate
    /// </summary>
    public StartActionCommandConsumer(ILogger<StartActionCommandConsumer> logger, IAgvRepository agvRepository)
    {
        _logger = logger;
        _agvRepository = agvRepository;
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
        string agvSerialNumber = context.Message.SerialNumber;
        var assignedAgv = context.Message.CorrelationId;

        if (string.IsNullOrEmpty(agvSerialNumber))
        {
            throw new AssignedAgvNotFoundException(assignedAgv, agvSerialNumber, action);
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
        // And we are transferring the Action's Guid
        await context.PublishMqtt(new MqttOrder(action.CorrelationId.ToString())
        {
            SerialNumber = agvSerialNumber,
            OrderType = orderType,
            OrderId = action.CorrelationId.ToString(),
            RecordType = OrderRecordType.Order,
            Stations = new Stations
            {
                FromStationIdentifier = sourceToSend,
                ToStationIdentifier = destinationToSend
            }
        }, context.CancellationToken);

        FeedAgvRepository(assignedAgv, action);
        _logger.LogInformation("Sent transport order: {OrderId} for AGV:{AssignedAgv}/{AgvId} at target: {TargetNodeId}",
            action.CorrelationId, assignedAgv, agvSerialNumber, targetNodeId);
    }

    private void FeedAgvRepository(Guid correlationId, AgvAction agvAction)
    {
        _agvRepository.AddOrUpdateAgvAction(correlationId, agvAction);
    }
}