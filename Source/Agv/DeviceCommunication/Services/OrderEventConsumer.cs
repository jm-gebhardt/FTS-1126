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
            _logger.LogTrace("Received {EventType} for {OrderId}", context.Message.EventType, context.Message.OrderId);

            var coralationId = context.Message.OrderId.ToGuid();

            switch (context.Message.EventType)
            {
                case OrderEventType.Started:
                    await context.Publish(new TransportOrderStarted(coralationId, context.Message.OrderId));
                    break;

                case OrderEventType.Picked:
                    await context.Publish(new TransportOrderPickCompleted(coralationId, context.Message.OrderId));
                    break;

                case OrderEventType.Completed:
                    await context.Publish(new TransportOrderCompleted(coralationId, context.Message.OrderId));
                    break;

                case OrderEventType.Cancelled:
                    await context.Publish(new TransportOrderCancelSucceeded(coralationId, context.Message.OrderId));
                    break;

                case OrderEventType.CancellFailed:
                    await context.Publish(new TransportOrderCancelFailed(coralationId, context.Message.OrderId, Common.Definitions.TransportOrderErrorCode.Any));
                    break;

                case OrderEventType.Failed:
                    await context.Publish(new TransportOrderFailed(coralationId, context.Message.OrderId, Common.Definitions.TransportOrderErrorCode.Any));
                    break;

                default:
                    _logger.LogWarning("Undefined EventType {EventType} for {OrderId}", context.Message.EventType, context.Message.OrderId);
                    break;
            }
        }
    }
}
