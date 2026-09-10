using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;

/// <summary>
/// Provides a request for the simulation configuration of a single agv type.
/// </summary>
/// <param name="CorrelationId">The id of the agv type.</param>
public record RequestAgvTypeConfiguration(Guid CorrelationId) : CorrelatedBy<Guid>;