using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;

/// <summary>
/// AGV position and/or velocity for visualization purposes.
/// Can be published at a higher rate than the state topic.
/// All fields are optional to reduce bandwidth.
/// <para>See "visualization.schema".</para>
/// </summary>
[PublicAPI]
[EntityName("amq.topic")] // Specifies the RabbitMq exchange where the message is published
[RoutingKey("*.*.*.*.visualization")] // Specifies the topic where subscribers can receive the message
public record MqttVisualization
{
    /// <summary>
    /// HeaderId of the message.
    /// The headerId is defined per topic and incremented by 1 with each sent (but not necessarily received) message.
    /// </summary>
    public int? HeaderId { get; set; }

    /// <summary>
    /// Timestamp in ISO8601 format (YYYY-MM-DDTHH:mm:ss.ffZ).
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Version of the protocol [Major].[Minor].[Patch].
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Manufacturer of the AGV.
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Serial number of the AGV.
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Current position of the AGV on the map.
    /// </summary>
    public MqttAgvPosition? AgvPosition { get; set; }

    /// <summary>
    /// The AGV's velocity in vehicle coordinates.
    /// </summary>
    public MqttVelocity? Velocity { get; set; }
}
