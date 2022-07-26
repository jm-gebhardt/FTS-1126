using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Events
{
    /// <summary>
    /// Transformation of <see cref="MqttMessages.MqttGraph"/> into MassTransit message.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record GraphReceived : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// See <see cref="MqttMessages.MqttGraph"/>.
        /// </summary>
        public MqttMessages.MqttGraph MqttGraph { get; }

        /// <summary>
        /// Build graph message.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="mqttGraph"></param>
        public GraphReceived(Guid correlationId, MqttMessages.MqttGraph mqttGraph)
        {
            CorrelationId = correlationId;
            MqttGraph = mqttGraph;
        }
    }
}
