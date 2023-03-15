using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Microsoft.Extensions.Logging;
using JetBrains.Annotations;
using MassTransit;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
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

                case OrderEventType.Picked:
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
}

/*

/// <summary>
/// Signals that an action has been started
/// </summary>
[PublicAPI]
public record ActionStartedEvent(Guid CorrelationId) : CorrelatedBy<Guid>;

/// <summary>
/// Signals that an action has been completed
/// </summary>
[PublicAPI]
public record ActionCompletedEvent(Guid CorrelationId) : CorrelatedBy<Guid>;

/// <summary>
/// Signals that an action has failed
/// </summary>
[PublicAPI]
public record ActionFailedEvent(Guid CorrelationId) : CorrelatedBy<Guid>;

/// <summary>
/// Signals that an action has been cancelled
/// </summary>
[PublicAPI]
public record ActionCancelledEvent(Guid CorrelationId) : CorrelatedBy<Guid>;

/// <summary>
/// Signals that an action cancelation has failed 
/// </summary>
[PublicAPI]
public record ActionCancelFailedEvent(Guid CorrelationId) : CorrelatedBy<Guid>;

*/
