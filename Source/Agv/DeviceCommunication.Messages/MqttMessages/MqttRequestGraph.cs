using Gebhardt.StoreWare.Agv.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Messages.MqttMessages;

/// <summary>
/// Requests the current graph from the AGV
/// <para>
/// See "request_graph.schema".</para></summary>
[PublicAPI]
[EntityName("amq.topic")] // Specifies the RabbitMq exchange where the message is published
[RoutingKey("agv.requestGraph")] // Specifies the topic where subscribers can receive the message
public record MqttRequestGraph
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
    /// Initializes a new instance of <see cref="MqttRequestGraph"/>
    /// </summary>
    public MqttRequestGraph(string serialNumber, Guid requestId)
    {
        SerialNumber = serialNumber;
        RequestId = requestId;
    }
}