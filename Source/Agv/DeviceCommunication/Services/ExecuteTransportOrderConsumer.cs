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
		IConsumer<IExecuteTransportOrder>
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
		/// Handle message <see cref="IExecuteTransportOrder"/>. and send an order to the AGV of Type Transport, RecordType Order
		/// </summary>
		/// <param name="context">Context of the consumer wrapping details surrounding the inbound message.</param>
		public async Task Consume(ConsumeContext<IExecuteTransportOrder> context)
		{
			await context.PublishMqtt<IOrder> (new
			{
				context.Message.Order.OrderId,
				Identifier = context.Message.AssignedAgv,
				OrderType = TransportOrderType.Transport.ToString(),
				RecordType = RecordType.Order.ToString(),
				Stations = new Stations()
				{
					FromStationIdentifier = context.Message.Order.Source, 
					ToStationIdentifier = context.Message.Order.Destination
				}
			}, context.CancellationToken);

			_logger.LogInformation("Sent transport order: {OrderId} for AGV:{AssignedAgv} from {Source} to {Destination}", context.Message.Order.OrderId, context.Message.AssignedAgv, context.Message.Order.Source, context.Message.Order.Destination);

		}
	}
}
