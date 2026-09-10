using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;

/// <summary>
/// Provides a Dto to change the simulation configuration of an agv type. A property that is omitted keeps its stored value.
/// </summary>
[PublicAPI]
public record UpdateAgvTypeConfigurationDto
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
    /// The battery charge in percent an agv starts with. Only affects agvs spawned afterwards.
    /// </summary>
    public int? InitialBatteryCharge { get; init; }

    /// <summary>
    /// The maximum deviation in percent applied to the initial battery charge. Only affects agvs spawned afterwards.
    /// </summary>
    public int? InitialBatteryChargeDeviation { get; init; }
}