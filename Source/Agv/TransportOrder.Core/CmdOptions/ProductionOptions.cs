using CommandLine;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Core.CmdOptions;

/// <summary>
/// Production Mode Options
/// </summary>
[UsedImplicitly]
[Verb("production", true)]
public sealed class ProductionOptions : SystemOptions
{
    /// <summary>
    /// This is required for `dotnet ef migrations` to work.
    /// </summary>
    [UsedImplicitly]
    [Option("applicationName", HelpText = "Ignore property only used by dotnet tools")]
    public string? ApplicationName { get; init; }
}