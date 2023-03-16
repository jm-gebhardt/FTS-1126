using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Microsoft.Extensions.Logging;
using JetBrains.Annotations;
using MassTransit;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;

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

            // Splitting OrderId string composed of two parts: [ActionGuid]<space>[OrderId]
            // Incoming from <see cref="StartActionCommandConsumer"/>
            // Temporary solution
            var actionId = new Guid(message.OrderId.Split(" ")[0]);
            string parentId = message.OrderId.Split(" ")[1];
            
            ActionStatusChangedType actionStatusChangedType;
            switch (message.OrderEventType)
            {
                case OrderEventType.Started:
                    actionStatusChangedType = ActionStatusChangedType.Started;
                    break;

                case OrderEventType.Picked:
                    actionStatusChangedType = ActionStatusChangedType.Picked;
                    break;

                case OrderEventType.Completed:
                    actionStatusChangedType = ActionStatusChangedType.Completed;
                    break;

                case OrderEventType.Cancelled:
                    actionStatusChangedType = ActionStatusChangedType.Cancelled;
                    break;

                case OrderEventType.CancelFailed:
                    actionStatusChangedType = ActionStatusChangedType.CancelFailed;
                    break;

                case OrderEventType.Failed:
                    actionStatusChangedType = ActionStatusChangedType.Failed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Undefined OrderEventType {message.OrderEventType} for {message.OrderId}");
            }

            await context.Publish(new ActionStatusChangedEvent(actionId, actionStatusChangedType, parentId));
        }
    }
}
