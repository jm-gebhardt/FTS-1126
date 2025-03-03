using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

/// <summary>
/// Requests to remove an AGV from a zone.
/// <para>
/// See "request_zone_remove_agv.schema".</para></summary>
[PublicAPI]
[EntityName("amq.topic")] // Specifies the RabbitMq exchange where the message is published
[RoutingKey("agv.requestZoneRemoveAgv")] // Specifies the topic where subscribers can receive the message
public class MqttRequestZoneRemoveAgv
{
    /// <summary>
    /// Serial number of the AGV to process the request.
    /// </summary>
    public string SerialNumber { get; set; }

    /// <summary>
    /// A unique ID for that request.
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// Serial number of the agv to remove.
    /// </summary>
    public string AgvToRemove { get; set; }

    /// <summary>
    /// The zone id to remove the agv from.
    /// </summary>
    public string ZoneId { get; set; }

    /// <summary>
    /// Build message.
    /// </summary>
    /// <param name="serialNumber">Serial number of the AGV.</param>
    /// <param name="requestId">A unique ID for the request.</param>
    /// <param name="agvToRemove">Serial number of the AGV to remove from the zone.</param>
    /// <param name="zoneId">Id of the zone to remove the agv from.</param>
    public MqttRequestZoneRemoveAgv(string serialNumber, Guid requestId, string agvToRemove, string zoneId)
    {
        SerialNumber = serialNumber;
        RequestId = requestId;
        AgvToRemove = agvToRemove;
        ZoneId = zoneId;
    }
}