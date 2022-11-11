using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Extensions;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages
{
    /// <summary>
    /// The current map from the AGV
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    [EntityName("amq.topic")]   // Specifies the RabbitMq exchange where the message is published
    [RoutingKey("agv.map")]     // Specifies the topic where subscribers can receive the message
    public record MqttMap
    {
        /// <summary>
        /// Serial number of the AGV.
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// The unique ID replied from the request.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// The UTC modification date of the map. Timestamp in ISO8601 format.
        /// </summary>
        public string? ModificationDate { get; set; }

        /// <summary>
        /// The unique ID of the map
        /// </summary>
        public string? MapId { get; set; }

        /// <summary>
        /// The base 64 encoded data of the png image
        /// </summary>
        public string? Data { get; set; }

        /// <summary>
        /// The resolution of the maps image. Expressed as how many [m] per pixel are represented
        /// </summary>
        public float Resolution { get; set; }

        /// <summary>
        /// The x offset of the map in [m] (from the left upper corner) where the mapped graph coordinate system starts
        /// </summary>
        public float XOffset { get; set; }

        /// <summary>
        /// The y offset of the map in [m] (from the left upper corner) where the mapped graph coordinate system starts
        /// </summary>
        public float YOffset { get; set; }

        /// <summary>
        /// Create basic instance
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <param name="requestId"></param>
        public MqttMap(string serialNumber, string requestId)
        {
            SerialNumber = serialNumber;
            RequestId = requestId;
        }
    }
}
