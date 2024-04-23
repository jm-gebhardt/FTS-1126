using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.Acc.DeviceCommunication.Messages.MqttMessages;

/// <summary>
/// The basic AGV data from the AGV
/// </summary>
[PublicAPI]
[EntityName("amq.topic")]
[RoutingKey("agv.agvData")]
public record MqttAgvData
{
    /// <summary>
    /// Serial number of the agv.
    /// </summary>
    public string SerialNumber { get; set; }

    /// <summary>
    /// The unique ID replied from the request.
    /// </summary>
    public Guid? RequestId { get; set; }

    /// <summary>
    /// The model name of the agv.
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// The type of the agv.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// The IP Address of the agv.
    /// </summary>
    public string? IpV4Address { get; set; }

    /// <summary>
    /// The length of the agv in [m].
    /// </summary>
    public double? Length { get; set; }

    /// <summary>
    /// The width of the agv in [m].
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    /// The maximum payload of the agv in [kg].
    /// </summary>
    public long? Payload { get; set; }

    /// <summary>
    /// The capacity of the agv battery in [Ah].
    /// </summary>
    public double BatteryCapacity { get; set; }

    /// <summary>
    /// The maximum velocity of the agv in straight direction [m/s].
    /// </summary>
    public double MaxVelocityStraight { get; set; }

    /// <summary>
    /// The maximum velocity of the agv in while turning [m/s].
    /// </summary>
    public double? MaxVelocityTurn { get; set; }

    /// <summary>
    /// The maximum velocity of the agv while turning on spot [rad/s].
    /// </summary>
    public double? MaxVelocityTurnOnSpot { get; set; }

    /// <summary>
    /// The threshold angle by which the agv will stop and turn on spot [rad]. 
    /// </summary>
    public double? TurnOnSpotThreshold { get; set; }

    /// <summary>
    /// The maximum acceleration of the agv [m/s^2].
    /// </summary>
    public double? Acceleration { get; set; }

    /// <summary>
    /// The list of sensors in the agv.
    /// </summary>
    public Sensor[] Sensors { get; set; }

    /// <summary>
    /// The software version of the agv.
    /// </summary>
    public string? SoftwareVersion { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="MqttAgvData"/>.
    /// </summary>
    public MqttAgvData(
        string serialNumber,
        string model,
        string type,
        string? ipV4Address,
        string? softwareVersion,
        double maxVelocityStraight,
        double batteryCapacity,
        double? length,
        double? width,
        double? acceleration,
        double? maxVelocityTurn,
        double? maxVelocityTurnOnSpot,
        double? turnOnSpotThreshold,
        long? payload,
        Sensor[] sensors)
    {
        SerialNumber = serialNumber;
        Model = model;
        Type = type;
        IpV4Address = ipV4Address;
        SoftwareVersion = softwareVersion;
        MaxVelocityStraight = maxVelocityStraight;
        BatteryCapacity = batteryCapacity;
        Length = length;
        Width = width;
        Acceleration = acceleration;
        MaxVelocityTurn = maxVelocityTurn;
        MaxVelocityTurnOnSpot = maxVelocityTurnOnSpot;
        TurnOnSpotThreshold = turnOnSpotThreshold;
        Payload = payload;
        Sensors = sensors;
    }
}

/// <summary>
/// A sensor object skeleton.
/// </summary>
[PublicAPI]
public record Sensor
{
    /// <summary>
    /// The identifier of the sensor.
    /// </summary>
    public string? SensorIdentifier { get; set; }
}