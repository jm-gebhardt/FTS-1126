using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
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
    public class ExecuteTransportOrderConsumer :
        IConsumer<ExecuteTransportOrder>,
        IConsumer<SendAgvChargeOrder>
    {
        private readonly ILogger<ExecuteTransportOrderConsumer> _logger;

        /// <summary>
        /// Create dummy consumer with message handler delegate
        /// </summary>
        public ExecuteTransportOrderConsumer(ILogger<ExecuteTransportOrderConsumer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Handle message <see cref="ExecuteTransportOrder"/>. and send an order to the AGV of Type Transport, RecordType Order
        /// </summary>
        /// <param name="context">Context of the consumer wrapping details surrounding the inbound message.</param>
        public async Task Consume(ConsumeContext<ExecuteTransportOrder> context)
        {
            await context.PublishMqtt(new MqttOrder(context.Message.OrderDetails.OrderId)
            {
                SerialNumber = context.Message.AgvId,
                OrderType = OrderType.Transport,
                RecordType = OrderRecordType.Order,
                Stations = new Stations()
                {
                    FromStationIdentifier = context.Message.OrderDetails.Source,
                    ToStationIdentifier = context.Message.OrderDetails.Destination
                }
            }, context.CancellationToken);

            _logger.LogInformation("Sent transport order: {OrderId} for AGV:{AgvId} from {Source} to {Destination}", context.Message.OrderDetails.OrderId, context.Message.AgvId, context.Message.OrderDetails.Source, context.Message.OrderDetails.Destination);
        }

        /// <summary>
        /// Handle message <see cref="SendAgvChargeOrder"/>. and send an order to the AGV of Type Charge, RecordType Order
        /// </summary>
        /// <param name="context">Context of the consumer wrapping details surrounding the inbound message.</param>
        public async Task Consume(ConsumeContext<SendAgvChargeOrder> context)
        {
            if (context.Message.CorrelationId == Guid.Empty)
            {
                _logger.LogWarning("Charge order for AGV:{AgvId} to {Destination} has no GUID!", context.Message.AgvId, context.Message.ToCharger);
            }
            await context.PublishMqtt(new MqttOrder(context.Message.CorrelationId.ToString()) //use correlation ID here, because we don't have an orderId here
            {
                SerialNumber = context.Message.AgvId,
                OrderType = OrderType.Charge,
                RecordType = OrderRecordType.Order,
                Stations = new Stations()
                {
                    ToStationIdentifier = context.Message.ToCharger
                }
            }, context.CancellationToken);

            _logger.LogInformation("Sent charge order: {CorrelationId} for AGV:{AgvId} to {Destination}", context.Message.CorrelationId, context.Message.AgvId, context.Message.ToCharger);
        }
    }
}
