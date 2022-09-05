using CommandLine;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.CmdOptions
{
    /// <summary>
    /// CommandLine Options for Simulation Mode
    /// </summary>
    [Verb("simulate", HelpText = "Run in simulation mode")]
    public class SimulateOptions
    {
        /// <summary>
        /// Interval for creating simulated HostOrders
        /// </summary>
        [UsedImplicitly]
        [Option('i', "interval", HelpText = "Interval for creating simulated HostOrder in [ms]", Default = 10000, Required = true)]
        public int Interval { get; init; }

        /// <summary>
        /// List of simulation sources
        /// </summary>
        [UsedImplicitly]
        [Option('s', "sources", HelpText = "List of simulation sources", Required = true, Min = 1, Separator = ',')]
        public IEnumerable<string>? Sources { get; init; }

        /// <summary>
        /// List of simulation destinations
        /// </summary>
        [UsedImplicitly]
        [Option('d', "destinations", HelpText = "List of simulation destinations", Required = true, Min = 1, Separator = ',')]
        public IEnumerable<string>? Destinations { get; init; }

        /// <summary>
        /// This is required for 'dotnet ef migrations' to work
        /// </summary>
        [UsedImplicitly]
        [Option("applicationName", HelpText = "Ignore property only used by dotnet tools")]
        public string? ApplicationName { get; init; }
    }
}