using Gebhardt.StoreWare.Agv.DeviceCommunication.Messages.MqttMessages;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Messages.Events;

/// <summary>
/// Transformation of <see cref="MqttGraph"/> into MassTransit message.
/// </summary>
[PublicAPI]
public record InternalGraphReceived : CorrelatedBy<Guid>
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
    public InternalGraphReceived(Guid correlationId, MqttGraph graph)
    {
        CorrelationId = correlationId;
        Graph = graph;
    }
}