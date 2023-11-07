using CommandLine;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;

/// <summary>
/// CommandLine Options for the SmartLinkCommunication
/// </summary>
public class CmdConfig
{
    /// <summary/>
    [Option('u', "url", HelpText = "The single SmartLink module base url", Required = false)]
    [UsedImplicitly]
    public string? ModuleUrl { get; init; }

    /// <summary/>
    [Option('p', "pin", HelpText = "The pin of the light barrier", Required = false)]
    [UsedImplicitly]
    public string? Pin { get; init; }

    /// <summary/>
    [Option('n', "nodes", HelpText = "The nodes for the pick and drop action as comma seperated list, e.g: P1,D2", Required = false)]
    [UsedImplicitly]
    public string? PickDropNode { get; init; }

    /// <summary/>
    [Option('t', "waitingTime", HelpText = "The waiting time for the stabilized pin until the action gets executed [s].", Required = false)]
    [UsedImplicitly]
    public int WaitingTime { get; init; }

    /// <summary/>
    [Option('c', "config", HelpText = "The path to the YAML config file. This is an alternative to the other command line arguments. If it is set, no other command line arguments are allowed.", Required = false)]
    [UsedImplicitly]
    public string? ConfigFilePath { get; init; }
}