using CommandLine;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions
{

    /// <summary>
    /// Saga Persistence Options
    /// </summary>
    [UsedImplicitly]
    public class SagaPersistenceOptions
    {
        /// <summary>
        /// 
        /// </summary>
        [UsedImplicitly]
        [Option(longName: "enableInMemoryPersistence", Default = false)]
        public bool EnableInMemoryPersistence { get; init; }
    }
}