using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Common.Messages;

/// <summary>
/// Provides the tunables of a simulated agv type. A member that is null keeps the stored value, or falls back to the default when there is none.
/// </summary>
[PublicAPI]
public record AgvTypeSimulationSettings
{
    /// <summary>
    /// The number of agvs simulated for this agv type.
    /// </summary>
    public int? NoOfAgvs { get; init; }

    /// <summary>
    /// The interval in milliseconds between two state publications.
    /// </summary>
    public int? StateUpdateIntervalMs { get; init; }

    /// <summary>
    /// The interval in milliseconds between two visualization publications.
    /// </summary>
    public int? VisualizationUpdateIntervalMs { get; init; }

    /// <summary>
    /// The battery discharge rate in percent per second.
    /// </summary>
    public double? BatteryDischargeRate { get; init; }

    /// <summary>
    /// The battery charge rate in percent per second.
    /// </summary>
    public double? BatteryChargeRate { get; init; }

    /// <summary>
    /// The battery charge in percent an agv starts with. Applied when an agv is spawned.
    /// </summary>
    public int? InitialBatteryCharge { get; init; }

    /// <summary>
    /// The maximum deviation in percent applied to the initial battery charge. Applied when an agv is spawned.
    /// </summary>
    public int? InitialBatteryChargeDeviation { get; init; }
}