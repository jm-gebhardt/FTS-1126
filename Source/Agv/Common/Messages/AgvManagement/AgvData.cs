using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

/// <summary>
/// AGV Data
/// </summary>
[PublicAPI]
public record AgvData : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The model name of the AGV
    /// </summary>
    public string Model { get; init; }

    /// <summary>
    /// The type of the AGV
    /// </summary>
    public string Type { get; init; }

    /// <summary>
    /// The IP Address of the AGV
    /// </summary>
    public string IpV4Address { get; init; }

    /// <summary>
    /// The software version of the AGV
    /// </summary>
    public string SoftwareVersion { get; init; }

    /// <summary>
    /// The maximum velocity of the AGV in straight direction [m/s]
    /// </summary>
    public double MaxVelocityStraight { get; init; }

    /// <summary>
    /// The capacity of the AGVs battery in [Ah]
    /// </summary>
    public double BatteryCapacity { get; init; }

    /// <summary>
    /// The length of the AGV in [m]
    /// </summary>
    public double Length { get; init; }

    /// <summary>
    /// The width of the AGV in [m]
    /// </summary>
    public double Width { get; init; }

    /// <summary>
    /// The maximum velocity of the AGV while turning on spot [m/s^2]
    /// </summary>
    public double Acceleration { get; init; }

    /// <summary>
    /// The maximum velocity of the AGV in while turning [m/s]
    /// </summary>
    public double MaxVelocityTurn { get; init; }

    /// <summary>
    /// The maximum velocity of the AGV while turning on spot [rad/s]
    /// </summary>
    public double MaxVelocityTurnOnSpot { get; init; }

    /// <summary>
    /// The threshold angle at which the agv must stop at the end of an edge in order to align itself with the angle for the next edge [rad]. 
    /// </summary>
    public double TurnOnSpotThreshold { get; init; }

    /// <summary>
    /// The max. payload of the AGV in [kg]
    /// </summary>
    public long Payload { get; init; }

    /// <summary>
    /// The list of sensors in the agv
    /// </summary>
    public Sensor[] Sensors { get; init; }

    /// <inheritdoc cref="AgvData"/>
    public AgvData(Guid correlationId,
        string model,
        string type,
        string ipV4Address,
        string softwareVersion,
        double batteryCapacity,
        double acceleration,
        double maxVelocityStraight,
        double maxVelocityTurn,
        double maxVelocityTurnOnSpot,
        double turnOnSpotThreshold,
        double width,
        double length,
        long payload,
        Sensor[] sensors
    )
    {
        CorrelationId = correlationId;
        Model = model;
        Type = type;
        IpV4Address = ipV4Address;
        SoftwareVersion = softwareVersion;
        Acceleration = acceleration;
        BatteryCapacity = batteryCapacity;
        MaxVelocityStraight = maxVelocityStraight;
        MaxVelocityTurn = maxVelocityTurn;
        MaxVelocityTurnOnSpot = maxVelocityTurnOnSpot;
        TurnOnSpotThreshold = turnOnSpotThreshold;
        Width = width;
        Length = length;
        Payload = payload;
        Sensors = sensors;
        TurnOnSpotThreshold = turnOnSpotThreshold;
    }

    /// <summary>
    /// A sensor object skeleton
    /// </summary>
    [PublicAPI]
    public class Sensor
    {
        /// <summary>
        /// The identifier of the sensor
        /// </summary>
        public string? SensorIdentifier { get; init; }
    }
}