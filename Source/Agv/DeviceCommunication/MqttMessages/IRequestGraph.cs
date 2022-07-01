using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Extensions;


namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages
{
    /// <summary>
    /// Requests the current graph from the AGV
    /// <para>
    /// See "request_graph.schema".</para></summary>
	[JetBrains.Annotations.PublicAPI]
    [EntityName("amq.topic")]           // Specifies the RabbitMq exchange where the message is published
    [RoutingKey("agv.request_graph")]   // Specifies the topic where subscribers can receive the message
    public interface IRequestGraph
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
