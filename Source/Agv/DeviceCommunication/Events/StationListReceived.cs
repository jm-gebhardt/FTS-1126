using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Events
{
    /// <summary>
    /// Transformation of <see cref="MqttMessages.MqttStationList"/> into MassTransit message.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record StationListReceived : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// See <see cref="MqttMessages.MqttStationList"/>.
        /// </summary>
        public MqttMessages.MqttStationList MqttStationList { get; }

        /// <summary>
        /// Build station list message.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="mqttStationList"></param>
        public StationListReceived(Guid correlationId, MqttMessages.MqttStationList mqttStationList)
        {
            CorrelationId = correlationId;
            MqttStationList = mqttStationList;
        }
    }
}
