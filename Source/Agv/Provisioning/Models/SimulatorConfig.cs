using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Provisioning.Models;

/// <summary>
/// The simulation configuration of one agv type.
/// </summary>
[PublicAPI]
public record SimulatorConfig
{
    /// <summary>
    /// The identifier of the agv type in the form manufacturer.seriesName.
    /// </summary>
    public string AgvTypeIdentifier { get; init; } = string.Empty;

    /// <summary>
    /// The tunables of the simulated agvs.
    /// </summary>
    public AgvTypeSimulationSettings Settings { get; init; } = new();
}