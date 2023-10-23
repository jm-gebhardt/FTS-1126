using CommandLine;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.CmdOptions;

/// <summary>
/// Production Mode Options
/// </summary>
[UsedImplicitly]
[Verb("production", true)]
public sealed class ProductionOptions
{
    /// <summary>
    /// This is required for `dotnet ef migrations` to work.
    /// </summary>
    [UsedImplicitly]
    [Option("applicationName", HelpText = "Ignore property only used by dotnet tools")]
    public string? ApplicationName { get; init; }

    /// <summary>
    /// Accept setting of AGV when entering State: Discovered
    /// </summary>
    /// <remarks>
    /// True: AGV is automatically released when Discovered;
    /// False: User input is required to accept AGV
    /// </remarks>
    [UsedImplicitly]
    [Option("autoAccept", HelpText = "Automatic release after AGV discovery", Default = false)]
    public bool AutoAccept { get; init; }
}