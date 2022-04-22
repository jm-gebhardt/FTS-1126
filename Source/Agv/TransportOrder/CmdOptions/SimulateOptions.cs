using CommandLine;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions
{
    /// <summary>
    /// RoundRobin AGV DispatcherOptions
    /// </summary>
    [Verb("simulate")]
    public class SimulateOptions
    {
        /// <summary>
        /// Number of AGVs
        /// </summary>
        [UsedImplicitly]
        [Option('c', "agvs", HelpText = "Number of simulated AGVs", Default=1)]
        public int Agvs { get;  init; }
    }
}