using Gebhardt.Acc.Common.Domain.Utilities;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;

/// <summary>
/// Describes how many AGVs of one AGV type are simulated and how they behave.
/// </summary>
[PublicAPI]
public record SimulatedAgvTypeConfiguration
{
    private const char IdentifierSeparator = '.';

    /// <summary>
    /// The interval in milliseconds between two state publications used when none is given.
    /// </summary>
    public const int DefaultStateUpdateIntervalMs = 5000;

    /// <summary>
    /// The interval in milliseconds between two visualization publications used when none is given.
    /// </summary>
    public const int DefaultVisualizationUpdateIntervalMs = 800;

    /// <summary>
    /// The battery discharge rate in percent per second used when none is given.
    /// </summary>
    public const double DefaultBatteryDischargeRate = 0.1;

    /// <summary>
    /// The battery charge rate in percent per second used when none is given.
    /// </summary>
    public const double DefaultBatteryChargeRate = 1.0;

    /// <summary>
    /// The battery charge in percent an AGV starts with when none is given.
    /// </summary>
    public const int DefaultInitialBatteryCharge = 75;

    /// <summary>
    /// The maximum deviation in percent applied to the initial battery charge when none is given.
    /// </summary>
    public const int DefaultInitialBatteryChargeDeviation = 5;

    /// <summary>
    /// The unique identifier of the configuration, derived from the AGV type identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The identifier of the simulated AGV type in the form manufacturer.seriesName.
    /// </summary>
    public string AgvTypeIdentifier { get; init; }

    /// <summary>
    /// The number of AGVs simulated for this AGV type.
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
    /// The battery charge in percent an AGV starts with. Applied when an AGV is spawned.
    /// </summary>
    public int InitialBatteryCharge { get; init; }

    /// <summary>
    /// The maximum deviation in percent applied to the initial battery charge. Applied when an AGV is spawned.
    /// </summary>
    public int InitialBatteryChargeDeviation { get; init; }

    /// <summary>
    /// The manufacturer, derived from the AGV type identifier.
    /// </summary>
    public string Manufacturer => IdentifierPart(AgvTypeIdentifier, 0);

    /// <summary>
    /// The series name, derived from the AGV type identifier.
    /// </summary>
    public string SeriesName => IdentifierPart(AgvTypeIdentifier, 1);

    /// <summary>For deserialization.</summary>
    public SimulatedAgvTypeConfiguration()
    {
        AgvTypeIdentifier = null!;
    }

    /// <inheritdoc cref="SimulatedAgvTypeConfiguration"/>
    public SimulatedAgvTypeConfiguration(string agvTypeIdentifier,
        int noOfAgvs = 0,
        int stateUpdateIntervalMs = DefaultStateUpdateIntervalMs,
        int visualizationUpdateIntervalMs = DefaultVisualizationUpdateIntervalMs,
        double batteryDischargeRate = DefaultBatteryDischargeRate,
        double batteryChargeRate = DefaultBatteryChargeRate,
        int initialBatteryCharge = DefaultInitialBatteryCharge,
        int initialBatteryChargeDeviation = DefaultInitialBatteryChargeDeviation)
    {
        Id = agvTypeIdentifier.ToGuid();
        AgvTypeIdentifier = agvTypeIdentifier;
        NoOfAgvs = noOfAgvs;
        StateUpdateIntervalMs = stateUpdateIntervalMs;
        VisualizationUpdateIntervalMs = visualizationUpdateIntervalMs;
        BatteryDischargeRate = batteryDischargeRate;
        BatteryChargeRate = batteryChargeRate;
        InitialBatteryCharge = initialBatteryCharge;
        InitialBatteryChargeDeviation = initialBatteryChargeDeviation;
    }

    private static string IdentifierPart(string? agvTypeIdentifier, int index)
    {
        string[] parts = agvTypeIdentifier?.Split(IdentifierSeparator, 2) ?? [];
        return parts.Length > index ? parts[index] : string.Empty;
    }

    /// <summary>
    /// The serial numbers of the AGVs simulated for this AGV type.
    /// </summary>
    /// <returns>One serial number per simulated AGV.</returns>
    public IEnumerable<string> SerialNumbers()
    {
        return Enumerable.Range(0, NoOfAgvs).Select(index => $"{SeriesName}{index}");
    }
}