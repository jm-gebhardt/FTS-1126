using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Exceptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

/// <summary>
/// Consumer for MqttOrderEvent messages
/// </summary>
public class OrderEventConsumer : IConsumer<MqttOrderEvent>
{
    private readonly ILogger<OrderEventConsumer> _logger;

    /// <summary>
    /// Consumer for MqttOrderEvent messages
    /// </summary>
    [PublicAPI]
    public OrderEventConsumer(ILogger<OrderEventConsumer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handle MqttOrderEvent messages
    /// </summary>
    public async Task Consume(ConsumeContext<MqttOrderEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received {MessageType}.{OrderEventType} for {OrderId}", nameof(MqttOrderEvent), message.OrderEventType, message.OrderId);

        var actionId = message.OrderId.ToGuid();
        var agvCorrelationId = (message.SerialNumber ?? throw new SerialNumberNotSetException(nameof(MqttOrderEvent))).ToGuid();

        switch (message.OrderEventType)
        {
            case OrderEventType.Started:
                await context.Publish(new ActionStarted(actionId, agvCorrelationId));
                break;

            case OrderEventType.Completed:
                await context.Publish(new ActionCompleted(actionId));
                break;

            case OrderEventType.Canceled:
                await context.Publish(new ActionCanceled(actionId));
                break;

            case OrderEventType.CancelFailed:
                await context.Publish(new ActionCancelFailed(actionId));
                break;

            case OrderEventType.Failed:
                await context.Publish(new ActionFailed(actionId));
                break;

            default:
                throw new ArgumentOutOfRangeException($"Undefined OrderEventType {message.OrderEventType} for {message.OrderId}");
        }
    }
}