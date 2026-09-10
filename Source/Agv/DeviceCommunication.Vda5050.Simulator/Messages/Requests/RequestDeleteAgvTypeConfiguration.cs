using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;

/// <summary>
/// Provides a request to delete the simulation configuration of an agv type.
/// </summary>
/// <param name="CorrelationId">The id of the agv type.</param>
public record RequestDeleteAgvTypeConfiguration(Guid CorrelationId) : CorrelatedBy<Guid>;