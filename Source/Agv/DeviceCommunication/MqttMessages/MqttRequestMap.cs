using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Extensions;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages
{
    /// <summary>
    /// Requests the current map from the AGV
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    [EntityName("amq.topic")]           // Specifies the RabbitMq exchange where the message is published
    [RoutingKey("agv.requestMap")]   // Specifies the topic where subscribers can receive the message
    public record MqttRequestMap
    {
        /// <summary>
        /// Serial number of the AGV.
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// A unique ID for that request.
        /// </summary>
        public Guid RequestId { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="MqttRequestMap"/>
        /// </summary>
        public MqttRequestMap(string serialNumber, Guid requestId)
        {
            SerialNumber = serialNumber;
            RequestId = requestId;
        }
    }
}
