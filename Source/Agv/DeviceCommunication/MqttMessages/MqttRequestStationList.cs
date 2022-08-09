using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Extensions;


namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages
{
    /// <summary>
    /// Requests the station list from the AGV
    /// <para>
    /// See "request_station_list.schema".</para></summary>
    [JetBrains.Annotations.PublicAPI]
    [EntityName("amq.topic")]                   // Specifies the RabbitMq exchange where the message is published
    [RoutingKey("agv.requestStationList")]    // Specifies the topic where subscribers can receive the message
    public class MqttRequestStationList
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
        /// Build message.
        /// </summary>
        /// <param name="serialNumber">Serial number of the AGV.</param>
        /// <param name="requestId">A unique ID for the request.</param>
        public MqttRequestStationList(string serialNumber, Guid requestId)
        {
            SerialNumber = serialNumber;
            RequestId = requestId;
        }
    }
}
