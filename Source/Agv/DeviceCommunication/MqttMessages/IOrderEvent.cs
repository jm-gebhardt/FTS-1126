using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages
{
	/// <summary>
	/// The order object to be sent to the TAR/Karis AGV (see 'order_event.schema')
	/// </summary>
	[PublicAPI]
	//Specifies the RabbitMq exchange where the message is published
	[EntityName("amq.topic")]
	//Specifies the topic where subscribers can receive the message
	[RoutingKey("agv.order_event")]
	public interface IOrderEvent
	{
		/// <summary>
		/// The unique serialnumber of the agv
		/// </summary>
		public string? SerialNumber { get; set; }

		/// <summary>
		/// The unique ID of the order
		/// </summary>
		public string OrderId { get; set; }

		/// <summary>
		/// The type of the order event. Started/Completed/Failed
		/// </summary>
		public OrderEventType? EventType { get; set; }
    }
}
