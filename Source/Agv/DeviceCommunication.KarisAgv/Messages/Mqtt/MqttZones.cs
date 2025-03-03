using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

/// <summary>
/// The current zones from the AGV
/// </summary>
[PublicAPI]
[EntityName("amq.topic")] // Specifies the RabbitMq exchange where the message is published
[RoutingKey("agv.zones")] // Specifies the topic where subscribers can receive the message
public record MqttZones
{
    /// <summary>
    /// Serial number of the AGV.
    /// </summary>
    public string SerialNumber { get; set; }

    /// <summary>
    /// The unique ID replied from the request.
    /// </summary>
    public string RequestId { get; set; }

    /// <summary>
    /// The zones.
    /// </summary>
    public List<MqttZone> Zones { get; set; }

    /// <summary>
    /// Create basic instance.
    /// </summary>
    /// <param name="serialNumber"></param>
    /// <param name="requestId"></param>
    /// <param name="zones"></param>
    public MqttZones(string serialNumber, string requestId, List<MqttZone> zones)
    {
        SerialNumber = serialNumber;
        RequestId = requestId;
        Zones = zones;
    }
}

/// <summary>
/// The zone of an agv.
/// </summary>
[PublicAPI]
public record MqttZone
{
    /// <summary>
    /// The polygon that defines the zone boundaries.
    /// </summary>
    public List<MqttPolygonPoint> Polygon { get; set; }

    /// <summary>
    /// The id of the zone.
    /// </summary>
    public string ZoneId { get; set; }

    /// <summary>
    /// The capacity of the zone.
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// The agvs currently occupying the zone.
    /// </summary>
    public List<string> Occupancy {get; set; }

    /// <summary>
    /// Create basic instance.
    /// </summary>
    public MqttZone(string zoneId, int capacity, List<MqttPolygonPoint> polygon, List<string> occupancy)
    {
        ZoneId = zoneId;
        Capacity = capacity;
        Polygon = polygon;
        Occupancy = occupancy;
    }
}

/// <summary>
/// Point on a polygon.
/// </summary>
/// <param name="X"></param>
/// <param name="Y"></param>
/// <param name="Z"></param>
[PublicAPI]
public record MqttPolygonPoint(double X, double Y, double? Z);