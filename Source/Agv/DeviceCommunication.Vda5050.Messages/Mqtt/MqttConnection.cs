using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;

/// <summary>
/// Connection state message of the AGV. Must be sent with the retain flag.
/// <list type="bullet">
///   <item>On connect: publish with <see cref="MqttConnectionState.Online"/>.</item>
///   <item>Last will: pre-configured with <see cref="MqttConnectionState.ConnectionBroken"/>, sent by the broker on unexpected disconnect.</item>
///   <item>Orderly disconnect: publish with <see cref="MqttConnectionState.Offline"/> before shutting down.</item>
/// </list>
/// <para>See "connection.schema".</para>
/// </summary>
[PublicAPI]
[EntityName("amq.topic")] // Specifies the RabbitMq exchange where the message is published
[RoutingKey("*.*.*.*.connection")] // Specifies the topic where subscribers can receive the message
public record MqttConnection: MqttHeader
{
    public MqttConnectionState ConnectionState { get; set; }

    /// <summary>
    /// Deconstructs <see cref="MqttConnection"/>
    /// </summary>
    public void Deconstruct(out uint headerId, out DateTimeOffset timestamp, out string version, out string manufacturer, out string serialNumber, out MqttConnectionState connectionState)
    {
        headerId = HeaderId;
        timestamp = Timestamp;
        version = Version;
        manufacturer = Manufacturer;
        serialNumber = SerialNumber;
        connectionState = ConnectionState;
    }
}

/// <summary>
/// Connection state between the AGV and the MQTT broker.
/// </summary>
[PublicAPI]
public enum MqttConnectionState
{
    /// <summary>Connection between AGV and broker is active.</summary>
    Online,

    /// <summary>Connection between AGV and broker has ended in an orderly fashion (e.g. shutdown, sleep).</summary>
    Offline,

    /// <summary>Connection between AGV and broker has unexpectedly ended (last will).</summary>
    ConnectionBroken
}
