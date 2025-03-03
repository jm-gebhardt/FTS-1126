using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

/// <summary>
/// Requests the zones from the AGV
/// <para>
/// See "request_zones.schema".</para></summary>
[PublicAPI]
[EntityName("amq.topic")] // Specifies the RabbitMq exchange where the message is published
[RoutingKey("agv.requestZones")] // Specifies the topic where subscribers can receive the message
public class MqttRequestZones
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
    /// Build message.
    /// </summary>
    /// <param name="serialNumber">Serial number of the AGV.</param>
    /// <param name="requestId">A unique ID for the request.</param>
    public MqttRequestZones(string serialNumber, Guid requestId)
    {
        SerialNumber = serialNumber;
        RequestId = requestId;
    }
}