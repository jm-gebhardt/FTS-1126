using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Events
{
    /// <summary>
    /// Transformation of <see cref="MqttGraph"/> into MassTransit message.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record MqttGraphReceived : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// See <see cref="MqttGraph"/>.
        /// </summary>
        public MqttGraph Graph { get; }

        /// <summary>
        /// Build graph message.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="graph"></param>
        public MqttGraphReceived(Guid correlationId, MqttGraph graph)
        {
            CorrelationId = correlationId;
            Graph = graph;
        }
    }
}
