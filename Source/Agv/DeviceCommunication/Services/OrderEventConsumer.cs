using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;
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

        switch (message.OrderEventType)
        {
            case OrderEventType.Started:
                await context.Publish(new ActionStartedEvent(actionId));
                break;

            case OrderEventType.Completed:
                await context.Publish(new ActionCompletedEvent(actionId));
                break;

            case OrderEventType.Cancelled:
                await context.Publish(new ActionCancelledEvent(actionId));
                break;

            case OrderEventType.CancelFailed:
                await context.Publish(new ActionCancelFailedEvent(actionId));
                break;

            case OrderEventType.Failed:
                await context.Publish(new ActionFailedEvent(actionId));
                break;

            default:
                throw new ArgumentOutOfRangeException($"Undefined OrderEventType {message.OrderEventType} for {message.OrderId}");
        }
    }
}