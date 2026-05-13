using CommandLine;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Configuration;

/// <summary>
/// CommandLine Options 
/// </summary>
// ReSharper disable StringLiteralTypo
[UsedImplicitly]
public sealed class Options
{
    /// <summary>
    /// Number of AGVs
    /// </summary>
    [UsedImplicitly]
    // ReSharper disable once StringLiteralTypo
    [Option('c', "agvs", HelpText = "Number of simulated AGVs", Default = 1)]
    // ReSharper disable once IdentifierTypo
    public int Agvs { get; init; }

    /// <summary>
    /// Battery discharge rate [%/s]
    /// </summary>
    [UsedImplicitly]
    [Option("batterydischargerate", HelpText = "Rate at which the battery charge decreases during simulation in [%/s]", Default = 0.1)]
    public double BatteryDischargeRate { get; init; }

    /// <summary>
    /// Battery charge rate [%/s]
    /// </summary>
    [UsedImplicitly]
    [Option("batterychargerate", HelpText = "Rate at which the battery charge increases during charge in [%/s]", Default = 1.0)]
    public double BatteryChargeRate { get; init; }

    /// <summary>
    /// Initial battery charge of AGV in [%]
    /// </summary>
    [UsedImplicitly]
    [Option("initialbatterycharge", HelpText = "Initial battery charge of AGV in [%]", Default = 75)]
    public int InitialBatteryCharge { get; init; }

    /// <summary>
    /// If set the initial battery charge of the agv will deviate in this range (+/-) from initial battery charge in [%]
    /// </summary>
    [UsedImplicitly]
    [Option("initialbatterychargedeviation", HelpText = "If set the initial battery charge of the agv will deviate in this range (+/-) from initial battery charge in [%]", Default = 0)]
    public int InitialBatteryChargeDeviation { get; init; }

    /// <summary>
    /// Base of the serial number
    /// </summary>
    [UsedImplicitly]
    [Option("serialNumberBase", HelpText = "Base of the serial number", Default = "simagv")]
    public string SerialNumberBase { get; init; } = "simagv";

    /// <summary>
    /// The map the agv is driving on.
    /// </summary>
    [UsedImplicitly]
    [Option("mapId", HelpText = "The id of the map the agvs are on.", Default = "map")]
    public string MapId { get; init; } = "map";

    /// <summary>
    /// Manufacturer
    /// </summary>
    [UsedImplicitly]
    [Option("manufacturer", HelpText = "The agv manufacturer.", Default = "gebhardt")]
    public string Manufacturer { get; init; } = "gebhardt";

    /// <summary>
    /// Update interval for the visualization message
    /// </summary>
    [UsedImplicitly]
    [Option("visualizationInterval", HelpText = "The interval for the visualization message [ms].", Default = 1000)]
    public int VisualizationUpdateInterval { get; init; } = 1000;

    /// <summary>
    /// Update interval for the state message
    /// </summary>
    [UsedImplicitly]
    [Option("stateInterval", HelpText = "The interval for the state message [ms].", Default = 30000)]
    public int StateUpdateInterval { get; init; } = 30_000;
}