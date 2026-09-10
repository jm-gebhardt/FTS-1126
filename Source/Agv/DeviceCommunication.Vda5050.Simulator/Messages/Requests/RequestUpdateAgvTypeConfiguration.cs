using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;

/// <summary>
/// Provides a request to update the simulation configuration of an agv type.
/// </summary>
/// <param name="CorrelationId">The id of the agv type.</param>
/// <param name="Settings">The tunables to change.</param>
public record RequestUpdateAgvTypeConfiguration(Guid CorrelationId, AgvTypeSimulationSettings Settings) : CorrelatedBy<Guid>;