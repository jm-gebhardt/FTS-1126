using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.Acc.DeviceCommunication.Messages.MqttMessages;

/// <summary>
/// Requests the base data from the AGV
/// <para>
/// See "request_data.schema".</para></summary>
[PublicAPI]
[EntityName("amq.topic")] // Specifies the RabbitMq exchange where the message is published
[RoutingKey("agv.requestAgvData")] // Specifies the topic where subscribers can receive the message
public record MqttRequestData
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
    /// Initializes a new instance of <see cref="MqttRequestData"/>
    /// </summary>
    public MqttRequestData(string serialNumber, Guid requestId)
    {
        SerialNumber = serialNumber;
        RequestId = requestId;
    }
}