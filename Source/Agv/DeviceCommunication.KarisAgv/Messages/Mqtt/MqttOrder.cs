using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

/// <summary>
/// The order object to be sent to the TAR/Karis AGV ((see 'order.schema'))
/// </summary>
[PublicAPI]
//Specifies the RabbitMq exchange where the message is published
[EntityName("amq.topic")]
//Specifies the topic where subscribers can receive the message
[RoutingKey("agv.order")]
public record MqttOrder
{
    /// <summary>
    /// The unique ID of the order
    /// </summary>
    public string OrderId { get; set; }

    /// <summary>
    /// The coordinates of the point where to go to
    /// </summary>
    public Coordinates? Coordinates { get; set; }

    /// <summary>
    /// The unique serial number of the agv
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// The type of the order. Pick/Drop/Transport
    /// </summary>
    public OrderType? OrderType { get; set; }

    /// <summary>
    /// The type of the order. Order/OrderUpdate/OrderCancel
    /// </summary>
    public OrderRecordType? RecordType { get; set; }

    /// <summary>
    /// The route to travel set by the master control expressed in nodes and edges
    /// </summary>
    public Route? Route { get; set; }

    /// <summary>
    /// The station object containing from and to station
    /// </summary>
    public Stations? Stations { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="MqttOrder"/>
    /// </summary>
    public MqttOrder(string orderId)
    {
        OrderId = orderId;
    }
}

/// <summary>
/// The coordinates of the point where to go to
/// </summary>
[PublicAPI]
public record Coordinates
{
    /// <summary>
    /// The theta angle to go to
    /// </summary>
    public double ToTheta { get; set; }

    /// <summary>
    /// The x coordinate to go to
    /// </summary>
    public double ToX { get; set; }

    /// <summary>
    /// The y coordinate to go to
    /// </summary>
    public double ToY { get; set; }
}

/// <summary>
/// The route to travel set by the master control expressed in nodes and edges
/// </summary>
[PublicAPI]
public record Route
{
    /// <summary>
    /// The list of nodes to use/travel over for the order
    /// </summary>
    public List<object> Nodes { get; set; } = [];

    /// <summary>
    /// The list of edges to use/travel over for the order
    /// </summary>
    public List<object> Edges { get; set; } = [];
}

/// <summary>
/// The station object containing from and to station
/// </summary>
[PublicAPI]
public record Stations
{
    /// <summary>
    /// The properties of the station where to pick from
    /// </summary>
    public StationInfo? FromStation { get; set; }

    /// <summary>
    /// The properties of the station where to drop
    /// </summary>
    public StationInfo? ToStation{ get; set; }
}

/// <summary>
/// The station properties for pick or drop action.
/// </summary>
public record StationInfo
{
    /// <summary>
    /// The unique identifier of the station.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Flag if agv should rotate before accessing the station.
    /// </summary>
    public bool Rotate { get; init; }

    /// <summary>
    /// The height where to pick (fromStation) or to drop (toStation) in [m].
    /// </summary>
    public double? LiftHeight { get; init; }
}