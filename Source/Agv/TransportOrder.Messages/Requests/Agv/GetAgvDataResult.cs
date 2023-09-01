namespace TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Defines a result for the GetAgvData request
/// </summary>
public record GetAgvDataResult(AgvData AgvData);

/// <summary>
/// Represents the AgvData in a message 
/// </summary>
public record AgvData
{
    /// <summary>The CorrelationId of the agv</summary>
    public Guid CorrelationId { get; init; }

    /// <summary>The model name of the AGV</summary>
    public string Model { get; init; }

    /// <summary>The type of the AGV</summary>
    public string Type { get; init; }

    /// <summary>The IP Address of the AGV</summary>
    public string? IpV4Address { get; init; }

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
    public Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvData.Sensor[] Sensors { get; init; }

    /// <param name="correlationId">The CorrelationId of the agv</param>
    /// <param name="model">The model name of the AGV</param>
    /// <param name="type">The type of the AGV</param>
    /// <param name="ipV4Address">The IP Address of the AGV</param>
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
    [JetBrains.Annotations.PublicAPI]
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
        Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.AgvData.Sensor[] sensors
    )
    {
        CorrelationId = correlationId;
        Model = model;
        Type = type;
        IpV4Address = ipV4Address;
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
    /// Automapper ctor
    /// </summary>
    public AgvData()
    {
        Model = null!;
        Type = null!;
        Sensors = null!;
    }
}