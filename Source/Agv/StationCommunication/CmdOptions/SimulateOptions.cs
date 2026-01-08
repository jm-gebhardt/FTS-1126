using CommandLine;
using JetBrains.Annotations;

namespace Gebhardt.Acc.StationCommunication.CmdOptions;

/// <summary>
/// CommandLine Options for Simulation Mode
/// </summary>
[Verb("simulate", HelpText = "Run in simulation mode")]
[UsedImplicitly]
public sealed class SimulateOptions;
