namespace Gebhardt.Acc.Vda5050Simulator.Domain;

public record AgvConfiguration(string Manufacturer, string SeriesName, string SerialNumber) 
    : AgvIdentity(Manufacturer, SeriesName, SerialNumber) 
{
    /// <summary>
    /// The interval in milliseconds between two state publications.
    /// </summary>
    public int StateUpdateIntervalMs { get; init; } = AgvTypeConfiguration.DefaultStateUpdateIntervalMs;

    /// <summary>
    /// The interval in milliseconds between two visualization publications.
    /// </summary>
    public int VisualizationUpdateIntervalMs { get; init; } = AgvTypeConfiguration.DefaultVisualizationUpdateIntervalMs;

    /// <summary>
    /// The battery discharge rate in percent per second.
    /// </summary>
    public double BatteryDischargeRate { get; init; } = AgvTypeConfiguration.DefaultBatteryDischargeRate;

    /// <summary>
    /// The battery charge rate in percent per second.
    /// </summary>
    public double BatteryChargeRate { get; init; } = AgvTypeConfiguration.DefaultBatteryChargeRate;

    /// <summary>
    /// The battery charge in percent the agv starts with. Applied when the agv is spawned.
    /// </summary>
    public int InitialBatteryCharge { get; init; } = AgvTypeConfiguration.DefaultInitialBatteryCharge;

    public int InitialBatteryChargeDeviation { get; init; } = AgvTypeConfiguration.DefaultInitialBatteryChargeDeviation;

    public double MaxSpeed { get; init; } = AgvTypeConfiguration.DefaultMaxSpeed;
}