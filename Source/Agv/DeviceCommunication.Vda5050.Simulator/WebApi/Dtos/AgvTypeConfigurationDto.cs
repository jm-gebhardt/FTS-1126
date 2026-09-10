using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;

/// <summary>
/// Provides a dto for the simulation configuration of an agv type.
/// </summary>
[PublicAPI]
public record AgvTypeConfigurationDto
{
    /// <summary>
    /// The id of the agv type.
    /// </summary>
    public Guid AgvTypeId { get; init; }

    /// <summary>
    /// The identifier of the agv type in the form manufacturer.seriesName.
    /// </summary>
    public string AgvTypeIdentifier { get; init; } = string.Empty;

    /// <summary>
    /// The manufacturer, derived from the agv type identifier.
    /// </summary>
    public string Manufacturer { get; init; } = string.Empty;

    /// <summary>
    /// The series name, derived from the agv type identifier.
    /// </summary>
    public string SeriesName { get; init; } = string.Empty;

    /// <summary>
    /// The serial numbers of the simulated agvs.
    /// </summary>
    public IReadOnlyCollection<string> SerialNumbers { get; init; } = [];

    /// <summary>
    /// The number of agvs simulated for this agv type.
    /// </summary>
    public int NoOfAgvs { get; init; }

    /// <summary>
    /// The interval in milliseconds between two state publications.
    /// </summary>
    public int StateUpdateIntervalMs { get; init; }

    /// <summary>
    /// The interval in milliseconds between two visualization publications.
    /// </summary>
    public int VisualizationUpdateIntervalMs { get; init; }

    /// <summary>
    /// The battery discharge rate in percent per second.
    /// </summary>
    public double BatteryDischargeRate { get; init; }

    /// <summary>
    /// The battery charge rate in percent per second.
    /// </summary>
    public double BatteryChargeRate { get; init; }

    /// <summary>
    /// The battery charge in percent an agv starts with. Applied when an agv is spawned.
    /// </summary>
    public int InitialBatteryCharge { get; init; }

    /// <summary>
    /// The maximum deviation in percent applied to the initial battery charge. Applied when an agv is spawned.
    /// </summary>
    public int InitialBatteryChargeDeviation { get; init; }
}