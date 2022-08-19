using Microsoft.Extensions.Logging;
using JetBrains.Annotations;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
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
            _logger.LogDebug("Received {MessageType}.{OrderEventType} for {OrderId}", nameof(MqttOrderEvent), context.Message.OrderEventType, context.Message.OrderId);

            var correlationId = context.Message.OrderId.ToGuid();

            switch (context.Message.OrderEventType)
            {
                case OrderEventType.Started:
                    await context.Publish(new TransportOrderStarted(correlationId, context.Message.OrderId));
                    break;

                case OrderEventType.Picked:
                    await context.Publish(new TransportOrderPickCompleted(correlationId, context.Message.OrderId));
                    break;

                case OrderEventType.Completed:
                    await context.Publish(new TransportOrderCompleted(correlationId, context.Message.OrderId));
                    break;

                case OrderEventType.Cancelled:
                    await context.Publish(new TransportOrderCancelSucceeded(correlationId, context.Message.OrderId));
                    break;

                case OrderEventType.CancelFailed:
                    await context.Publish(new TransportOrderCancelFailed(correlationId, context.Message.OrderId, TransportOrderErrorCode.Any));
                    break;

                case OrderEventType.Failed:
                    await context.Publish(new TransportOrderFailed(correlationId, context.Message.OrderId, TransportOrderErrorCode.Any));
                    break;

                default:
                    _logger.LogWarning("Undefined OrderEventType {OrderEventType} for {OrderId}", context.Message.OrderEventType, context.Message.OrderId);
                    break;
            }
        }
    }
}
