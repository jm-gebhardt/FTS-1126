using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Extensions;


namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages
{
    /// <summary>
    /// Requests the base data from the AGV
    /// <para>
    /// See "request_data.schema".</para></summary>
	[JetBrains.Annotations.PublicAPI]
    [EntityName("amq.topic")]           // Specifies the RabbitMq exchange where the message is published
    [RoutingKey("agv.request_data")]   // Specifies the topic where subscribers can receive the message
    public interface IRequestData
    {
        /// <summary>
        /// Serial number of the AGV.
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// A unique ID for the request
        /// </summary>
        public int RequestId { get; set; }
    }
}
