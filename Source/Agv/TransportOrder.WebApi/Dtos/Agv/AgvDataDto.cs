using JetBrains.Annotations;
using static Gebhardt.Acc.Common.Domain.Models.AgvData;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

/// <summary>
/// Agv Data model
/// </summary>
public record AgvDataDto
{
    /// <summary>The CorrelationId of the agv</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>The model name of the AGV</summary>
    public string Model { get; init; }

    /// <summary>The type of the AGV</summary>
    public string Type { get; init; }

    /// <summary>The IP Address of the AGV</summary>
    public string? IpV4Address { get; init; }

    /// <summary>The software version of the AGV</summary>
    public string SoftwareVersion { get; init; }

    /// <summary>The capacity of the AGVs battery in [Ah]</summary>
    public double BatteryCapacity { get; init; }

    /// <summary>The maximum velocity of the AGV while turning on spot [m/s^2]</summary>
    public double? Acceleration { get; init; }

    /// <summary>The maximum velocity of the AGV in straight direction [m/s]</summary>
    public double MaxVelocityStraight { get; init; }

    /// <summary>The maximum velocity of the AGV in while turning [m/s]</summary>
    public double? MaxVelocityTurn { get; init; }

    /// <summary>The maximum velocity of the AGV while turning on spot [rad/s]</summary>
    public double? MaxVelocityTurnOnSpot { get; init; }

    /// <summary>The width of the AGV in [m]</summary>
    public double? Width { get; init; }

    /// <summary>The length of the AGV in [m]</summary>
    public double? Length { get; init; }

    /// <summary>The max. payload of the AGV in [kg]</summary>
    public long? Payload { get; init; }

    /// <summary>The list of sensors in the agv</summary>
    public Sensor[] Sensors { get; init; }

    /// <inheritdoc cref="AgvDataDto"/>
    /// <param name="correlationId">The CorrelationId of the agv</param>
    /// <param name="model">The model name of the AGV</param>
    /// <param name="type">The type of the AGV</param>
    /// <param name="ipV4Address">The IP Address of the AGV</param>
    /// <param name="softwareVersion">The software version of the AGV</param>
    /// <param name="batteryCapacity">The capacity of the AGVs battery in [Ah]</param>
    /// <param name="acceleration">The maximum velocity of the AGV while turning on spot [m/s^2]</param>
    /// <param name="maxVelocityStraight">The maximum velocity of the AGV in straight direction [m/s]</param>
    /// <param name="maxVelocityTurn">The maximum velocity of the AGV in while turning [m/s]</param>
    /// <param name="maxVelocityTurnOnSpot">The maximum velocity of the AGV while turning on spot [rad/s]</param>
    /// <param name="width">The width of the AGV in [m]</param>
    /// <param name="length">The length of the AGV in [m]</param>
    /// <param name="payload">The max. payload of the AGV in [kg]</param>
    /// <param name="sensors">The list of sensors in the agv</param>
    /// <returns></returns>
    [PublicAPI]
    public AgvDataDto(
        Guid correlationId,
        string model,
        string type,
        string? ipV4Address,
        string softwareVersion,
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
        SoftwareVersion = softwareVersion;
        BatteryCapacity = batteryCapacity;
        Acceleration = acceleration;
        MaxVelocityStraight = maxVelocityStraight;
        MaxVelocityTurn = maxVelocityTurn;
        MaxVelocityTurnOnSpot = maxVelocityTurnOnSpot;
        Width = width;
        Length = length;
        Payload = payload;
        Sensors = sensors;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    public AgvDataDto()
    {
        Model = null!;
        Type = null!;
        SoftwareVersion = null!;
        Sensors = null!;
    }
}