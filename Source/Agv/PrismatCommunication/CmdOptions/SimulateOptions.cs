using CommandLine;
using JetBrains.Annotations;

namespace Gebhardt.Acc.PrismatCommunication.CmdOptions;

/// <summary>
/// CommandLine Options for Simulation Mode
/// </summary>
[Verb("simulate", HelpText = "Run in simulation mode")]
public sealed class SimulateOptions
{
    /// <summary>
    /// Interval for creating simulated HostOrders
    /// </summary>
    [UsedImplicitly]
    [Option('s', "speed", HelpText = "Speed factor of the simulation", Default = 1, Required = true)]
    public int Speed { get; init; }

    /// <summary>
    /// This is required for 'dotnet ef migrations' to work
    /// </summary>
    [UsedImplicitly]
    [Option("applicationName", HelpText = "Ignore property only used by dotnet tools")]
    public string? ApplicationName { get; init; }
}