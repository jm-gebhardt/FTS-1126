using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;

/// <summary>
/// Provides a request to add the simulation configuration of an agv type.
/// </summary>
/// <param name="CorrelationId">The id of the agv type.</param>
/// <param name="AgvTypeIdentifier">The identifier of the agv type in the form manufacturer.seriesName.</param>
/// <param name="Settings">The tunables of the simulated agvs.</param>
public record RequestAddAgvTypeConfiguration(Guid CorrelationId, string AgvTypeIdentifier, AgvTypeSimulationSettings Settings) : CorrelatedBy<Guid>;