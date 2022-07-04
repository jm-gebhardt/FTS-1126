using Microsoft.Extensions.Logging;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
    /// <summary>
    /// Consumer for IOrderEvent messages
    /// </summary>
    public class OrderEventConsumer : IConsumer<IOrderEvent>
    {
        private readonly ILogger<OrderEventConsumer> _logger;

        /// <summary>
        /// Consumer for IOrderEvent messages
        /// </summary>
        [PublicAPI]
        public OrderEventConsumer(ILogger<OrderEventConsumer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Handle IOrderEvent messages
        /// </summary>
        public async Task Consume(ConsumeContext<IOrderEvent> context)
        {
            _logger.LogTrace("Received {EventType} for {OrderId}", context.Message.EventType, context.Message.OrderId);

            switch (context.Message.EventType)
            {
                case OrderEventType.Started:
                    await context.Publish<ITransportOrderStarted>(new { context.Message.OrderId });
                    break;

                case OrderEventType.Picked:
                    await context.Publish<ITransportOrderPickCompleted>(new { context.Message.OrderId });
                    break;

                case OrderEventType.Completed:
                    await context.Publish<ITransportOrderCompleted>(new { context.Message.OrderId });
                    break;

                case OrderEventType.Cancelled:
                    await context.Publish<ITransportOrderCancelSucceeded>(new { context.Message.OrderId });
                    break;

                case OrderEventType.CancellFailed:
                    await context.Publish<ITransportOrderCancelFailed>(new { context.Message.OrderId });
                    break;

                case OrderEventType.Failed:
                    await context.Publish<ITransportOrderFailed>(new { context.Message.OrderId });
                    break;

                default:
                    _logger.LogWarning("Undefined EventType {EventType} for {OrderId}", context.Message.EventType, context.Message.OrderId);
                    break;
            }
        }
    }
}