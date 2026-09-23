using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Common.Messages;

/// <summary>
/// Represents a command to store the simulation configuration of an agv type unless it is already configured.
/// </summary>
/// <param name="CorrelationId">The identifier of the agv type.</param>
/// <param name="AgvTypeIdentifier">The identifier of the agv type in the form manufacturer.seriesName.</param>
/// <param name="Settings">The tunables of the simulated agvs.</param>
[PublicAPI]
public record EnsureSimulatedAgvTypeConfiguration(Guid CorrelationId, string AgvTypeIdentifier, AgvTypeSimulationSettings Settings) : CorrelatedBy<Guid>;