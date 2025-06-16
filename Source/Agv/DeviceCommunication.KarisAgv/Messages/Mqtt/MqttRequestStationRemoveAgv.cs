using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

/// <summary>
/// Requests to remove an AGV from a station.
/// <para>
/// See "request_station_remove_agv.schema".</para></summary>
[PublicAPI]
[EntityName("amq.topic")] // Specifies the RabbitMq exchange where the message is published
[RoutingKey("agv.requestStationRemoveAgv")] // Specifies the topic where subscribers can receive the message
public class MqttRequestStationRemoveAgv
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
    /// The station id to remove the agv from.
    /// </summary>
    public string StationId { get; set; }

    /// <summary>
    /// Build message.
    /// </summary>
    /// <param name="serialNumber">Serial number of the AGV.</param>
    /// <param name="requestId">A unique ID for the request.</param>
    /// <param name="agvToRemove">Serial number of the AGV to remove from the station.</param>
    /// <param name="stationId">Id of the station to remove the agv from.</param>
    public MqttRequestStationRemoveAgv(string serialNumber, Guid requestId, string agvToRemove, string stationId)
    {
        SerialNumber = serialNumber;
        RequestId = requestId;
        AgvToRemove = agvToRemove;
        StationId = stationId;
    }
}