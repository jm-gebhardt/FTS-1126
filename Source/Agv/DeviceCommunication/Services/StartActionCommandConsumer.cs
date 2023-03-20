using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
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
        public async Task Consume(ConsumeContext<StartActionCommand> context)
        {
            var action = context.Message.Action;
            var actionType = action.ActionType;
            OrderType orderType;
            string sourceToSend = string.Empty;
            string destinationToSend = string.Empty;

            if (actionType == ActionType.Pick)
            {
                orderType = OrderType.Pick;
                sourceToSend = action.Destination;
            }
            else if (actionType == ActionType.Drop)
            {
                orderType = OrderType.Drop;
                destinationToSend = action.Destination;
            }
            else
            {
                throw new NotImplementedException($"Can't handle {actionType} yet");
            }

            // We are using the action correlation Id as "order Id"
            // And we transferring the Action's Guid and its parenting OrderId through the same string 
            // This message will be split in <see cref="OrderEventConsumer"/>
            // Temporary solution
            await context.PublishMqtt(new MqttOrder(action.CorrelationId + " " + action.OrderId)
            {
                SerialNumber = action.AgvId,
                OrderType = orderType,
                RecordType = OrderRecordType.Order,
                Stations = new Stations
                {
                    FromStationIdentifier = sourceToSend,
                    ToStationIdentifier = destinationToSend
                }
            }, context.CancellationToken);

            FeedAgvRegistry(action.AgvCorrelationId, new AgvAction(action.CorrelationId, action.AgvId, action.ActionType, action.OrderId, action.ParentId, action.Source, action.Destination));
            _logger.LogInformation("Sent transport order: {OrderId} for AGV:{AgvId} from {Source} to {Destination}",
                action.OrderId, action.AgvId, action.Source, action.Destination);
        }

        private void FeedAgvRegistry(Guid correlationId, AgvAction agvAction)
        {
            _agvRegistry.AddOrUpdateAgvAction(correlationId, agvAction);
        }
    }
}
