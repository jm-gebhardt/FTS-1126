using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

/// <summary>
/// The last will from the AGV
/// </summary>
[PublicAPI]
[EntityName("amq.topic")]
[RoutingKey("#.connection")]
public record MqttAgvLastWill
{
    /// <inheritdoc cref="MqttAgvLastWill"/>/>>
    public MqttAgvLastWill(uint headerId, DateTimeOffset timeStamp, string version, string serialNumber, string manufacturer, ConnectionState connectionState)
    {
        HeaderId = headerId;
        Timestamp = timeStamp;
        Version = version;
        SerialNumber = serialNumber;
        Manufacturer = manufacturer;
        ConnectionState = connectionState;
    }

    /// <summary>
    /// The header id of the message.
    /// </summary>
    public uint HeaderId { get; set; }

    /// <summary>
    /// Timestamp of the last will
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Version of the protocol
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Serial number of the agv.
    /// </summary>
    public string SerialNumber { get; set; }

    /// <summary>
    /// Manufacturer of the agv.
    /// </summary>
    public string Manufacturer { get; set; }

    /// <summary>
    /// The connection state.
    /// </summary>
    public ConnectionState ConnectionState { get; set; }

    /// <summary>
    /// Deconstruct <see cref="MqttAgvLastWill"/>
    /// </summary>
    public void Deconstruct(out uint headerId, out DateTimeOffset timeStamp, out string version, out string serialNumber, out string manufacturer, out ConnectionState connectionState)
    {
        headerId = HeaderId;
        timeStamp = DateTimeOffset.UtcNow;
        version = Version;
        serialNumber = SerialNumber;
        manufacturer = Manufacturer;
        connectionState = ConnectionState;
    }
}

