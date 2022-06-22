using CommandLine;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions
{
    /// <summary>
    /// RoundRobin AGV DispatcherOptions
    /// </summary>
    [Verb("simulate")]
    public class SimulateOptions: SagaPersistenceOptions
    {
        /// <summary>
        /// Number of AGVs
        /// </summary>
        [UsedImplicitly]
        // ReSharper disable once StringLiteralTypo
        [Option('c', "agvs", HelpText = "Number of simulated AGVs", Default=1)]
        // ReSharper disable once IdentifierTypo
        public int Agvs { get;  init; }
    }
}