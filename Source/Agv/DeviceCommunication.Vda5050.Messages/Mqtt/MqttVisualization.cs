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
public record MqttVisualization : MqttHeader
{
    /// <summary>
    /// Current position of the AGV on the map.
    /// </summary>
    public MqttAgvPosition? AgvPosition { get; set; }

    /// <summary>
    /// The AGV's velocity in vehicle coordinates.
    /// </summary>
    public MqttVelocity? Velocity { get; set; }
}
