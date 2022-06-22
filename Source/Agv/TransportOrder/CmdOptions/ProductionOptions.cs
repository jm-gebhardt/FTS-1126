using CommandLine;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions
{
    /// <summary>
    /// Production Mode Options
    /// </summary>
    [UsedImplicitly]
    [Verb("production", isDefault: true)]
    public sealed class ProductionOptions: SagaPersistenceOptions
    {
        /// <summary>
        /// This is required for `dotnet ef migrations` to work.
        /// </summary>
        [UsedImplicitly]
        [Option("applicationName", HelpText="Ignore property only used by dotnet tools")]
        public string? ApplicationName { get; init; }
     
    }
}