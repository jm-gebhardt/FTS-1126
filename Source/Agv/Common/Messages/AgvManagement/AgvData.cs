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
    public Guid CorrelationId { get; }

    /// <summary>
    /// The model name of the AGV
    /// </summary>
    public string Model { get; }

    /// <summary>
    /// The type of the AGV
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// The IP Address of the AGV
    /// </summary>
    public string? IpV4Address { get; }

    /// <summary>
    /// The maximum velocity of the AGV in straight direction [m/s]
    /// </summary>
    public double MaxVelocityStraight { get; }

    /// <summary>
    /// The capacity of the AGVs battery in [Ah]
    /// </summary>
    public double BatteryCapacity { get; }

    /// <summary>
    /// The length of the AGV in [m]
    /// </summary>
    public double? Length { get; }

    /// <summary>
    /// The width of the AGV in [m]
    /// </summary>
    public double? Width { get; }

    /// <summary>
    /// The maximum velocity of the AGV while turning on spot [m/s^2]
    /// </summary>
    public double? Acceleration { get; }

    /// <summary>
    /// The maximum velocity of the AGV in while turning [m/s]
    /// </summary>
    public double? MaxVelocityTurn { get; }

    /// <summary>
    /// The maximum velocity of the AGV while turning on spot [rad/s]
    /// </summary>
    public double? MaxVelocityTurnOnSpot { get; }

    /// <summary>
    /// The max. payload of the AGV in [kg]
    /// </summary>
    public long? Payload { get; }

    /// <summary>
    /// The list of sensors in the agv
    /// </summary>
    public Sensor[] Sensors { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="AgvData"/>
    /// </summary>
    public AgvData(
        Guid correlationId,
        string model,
        string type,
        string? ipV4Address,
        double batteryCapacity,
        double? acceleration,
        double maxVelocityStraight,
        double? maxVelocityTurn,
        double? maxVelocityTurnOnSpot,
        double? width,
        double? length,
        long? payload,
        Sensor[] sensors
    )
    {
        CorrelationId = correlationId;
        Model = model;
        Type = type;
        IpV4Address = ipV4Address;
        Acceleration = acceleration;
        BatteryCapacity = batteryCapacity;
        MaxVelocityStraight = maxVelocityStraight;
        MaxVelocityTurn = maxVelocityTurn;
        MaxVelocityTurnOnSpot = maxVelocityTurnOnSpot;
        Width = width;
        Length = length;
        Payload = payload;
        Sensors = sensors;
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
        public string? SensorIdentifier { get; set; }
    }
}