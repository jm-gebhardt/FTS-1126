using CommandLine;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions
{
    /// <summary>
    /// CommandLine Options for Simulation Mode
    /// </summary>
    [JetBrains.Annotations.UsedImplicitly]
    [Verb("simulate", HelpText = "Run in simulation mode")]
    public sealed class SimulateOptions
    {
        /// <summary>
        /// Number of AGVs
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        // ReSharper disable once StringLiteralTypo
        [Option('c', "agvs", HelpText = "Number of simulated AGVs", Default=1)]
        // ReSharper disable once IdentifierTypo
        public int Agvs { get; init; }

        /// <summary>
        /// List of simulation sources
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        [Option('s', "sources", HelpText = "List of simulation sources", Required = true, Min = 1, Separator = ',')]
        public IEnumerable<string>? Sources { get; init; }

        /// <summary>
        /// List of simulation destinations
        /// </summary>  
        [JetBrains.Annotations.UsedImplicitly]
        [Option('d', "destinations", HelpText = "List of simulation destinations", Required = true, Min = 1, Separator = ',')]
        public IEnumerable<string>? Destinations { get; init; }

        /// <summary>
        /// Number of grid rows [3..[
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        // ReSharper disable once StringLiteralTypo
        [Option('r', "gridrows", HelpText = "Number of grid rows of simulated map [3..[", Default = 3)]
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

        /// <summary>
        /// Node travel time of the agv in [ms]
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        // ReSharper disable once StringLiteralTypo
        [Option('n', "nodetraveltime", HelpText = "The travel time for the simulated AGV from one node to another in [ms]", Default=5000)]
        public int NodeTravelTime { get; init; }

        /// <summary>
        /// Battery discharge rate [%/s]
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        // ReSharper disable once StringLiteralTypo
        [Option("batterydischargerate", HelpText = "Rate at which the battery charge decreases during simulation in [%/s]", Default = 0.1)]
        public double BatteryDischargeRate { get; init; }

        /// <summary>
        /// Battery charge rate [%/s]
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        // ReSharper disable once StringLiteralTypo
        [Option("batterychargerate", HelpText = "Rate at which the battery charge increases during charge in [%/s]", Default = 1.0)]
        public double BatteryChargeRate { get; init; }
    }
}
