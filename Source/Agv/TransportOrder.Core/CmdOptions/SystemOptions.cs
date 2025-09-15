using CommandLine;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Core.CmdOptions;

/// <summary>
/// Base options for system configuration.
/// </summary>
public class SystemOptions
{
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