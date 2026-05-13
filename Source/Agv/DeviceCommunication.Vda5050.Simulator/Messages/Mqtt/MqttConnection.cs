using JetBrains.Annotations;
using MassTransit;

// ReSharper disable InconsistentNaming

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;

/// <summary>
/// Connection state message of the AGV. Must be sent with the retain flag.
/// <list type="bullet">
///   <item>On connect: publish with <see cref="MqttConnectionState.ONLINE"/>.</item>
///   <item>Last will: pre-configured with <see cref="MqttConnectionState.CONNECTIONBROKEN"/>, sent by the broker on unexpected disconnect.</item>
///   <item>Orderly disconnect: publish with <see cref="MqttConnectionState.OFFLINE"/> before shutting down.</item>
/// </list>
/// <para>See "connection.schema".</para>
/// </summary>
[PublicAPI]
[EntityName("amq.topic")] // Specifies the RabbitMq exchange where the message is published
public record MqttConnection
{
    /// <summary>
    /// HeaderId of the message.
    /// The headerId is defined per topic and incremented by 1 with each sent (but not necessarily received) message.
    /// </summary>
    public uint HeaderId { get; set; }

    /// <summary>
    /// Timestamp in ISO8601 format (YYYY-MM-DDTHH:mm:ss.ffZ).
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Version of the protocol [Major].[Minor].[Patch].
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Manufacturer of the AGV.
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Serial number of the AGV.
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Current connection state between the AGV and the broker.
    /// </summary>
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
    ONLINE,

    /// <summary>Connection between AGV and broker has ended in an orderly fashion (e.g. shutdown, sleep).</summary>
    OFFLINE,

    /// <summary>Connection between AGV and broker has unexpectedly ended (last will).</summary>
    CONNECTIONBROKEN
}
