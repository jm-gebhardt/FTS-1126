using CommandLine;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;

/// <summary>
/// CommandLine Options for Simulation Mode
/// </summary>
// ReSharper disable StringLiteralTypo
[UsedImplicitly]
[Verb("simulate", HelpText = "Run in simulation mode")]
public sealed class SimulateOptions
{
    private const string GridGraphSet = "GridGraph";
    private const string FixedGraphSet = "FixedGraph";

    /// <summary>
    /// Number of AGVs
    /// </summary>
    [UsedImplicitly]
    // ReSharper disable once StringLiteralTypo
    [Option('c', "agvs", HelpText = "Number of simulated AGVs", Default = 1)]
    // ReSharper disable once IdentifierTypo
    public int Agvs { get; init; }

    /// <summary>
    /// The factor applied to agv velocities and acceleration to speed up the simulation.
    /// </summary>
    [UsedImplicitly]
    [Option('n', "simulationspeed", HelpText = "The factor applied to agv velocities and acceleration to control the speed of the simulation", Default = 1)]
    public int SimulationSpeed { get; init; }

    /// <summary>
    /// Cargo loading time [s].
    /// </summary>
    [UsedImplicitly]
    [Option("cargoloadingtime", HelpText = "Time span [s] needed to load cargo onto an agv", Default = 10)]
    public int CargoLoadingTime { get; init; }

    /// <summary>
    ///  Cargo unloading time [s].
    /// </summary>
    [UsedImplicitly]
    [Option("cargounloadingtime", HelpText = "Time span [s] needed to unload cargo from an agv", Default = 10)]
    public int CargoUnloadingTime { get; init; }

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
    /// Flag to use the grid graph generator in simulation mode
    /// </summary>
    [UsedImplicitly]
    // ReSharper disable once StringLiteralTypo
    [Option('f', "fixedgraph", HelpText = "Use the fixed graph generator", SetName = FixedGraphSet, Default = false)]
    public bool FixedGraph { get; init; }

    /// <summary>
    /// Flag to use the grid graph generator in simulation mode
    /// </summary>
    [UsedImplicitly]
    [Option('g', "gridgraph", HelpText = "Use the grid graph generator", SetName = GridGraphSet, Default = false)]
    public bool GridGraph { get; init; }

    /// <summary>
    /// List of simulation sources
    /// </summary>
    [UsedImplicitly]
    [Option('s', "sources", HelpText = "List of simulation sources", Required = false, Min = 1, Separator = ',', SetName = GridGraphSet)]
    public IEnumerable<string>? Sources { get; init; }

    /// <summary>
    /// List of simulation destinations
    /// </summary>  
    [UsedImplicitly]
    [Option('d', "destinations", HelpText = "List of simulation destinations", Required = false, Min = 1, Separator = ',', SetName = GridGraphSet)]
    public IEnumerable<string>? Destinations { get; init; }

    /// <summary>
    /// Number of grid rows [3..[
    /// </summary>
    [UsedImplicitly]
    [Option('r', "gridrows", HelpText = "Number of grid rows of simulated map [3..[", Default = 3, SetName = GridGraphSet)]
    public int GridRows
    {
        get => _gridRows;
        init
        {
            if (value < 3)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Number of grid rows need to be greater or equal to 3.");
            }
            _gridRows = value;
        }
    }

    private readonly int _gridRows;
}