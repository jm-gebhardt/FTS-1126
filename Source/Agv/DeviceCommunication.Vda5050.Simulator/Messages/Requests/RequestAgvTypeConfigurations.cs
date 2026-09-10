using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;

/// <summary>
/// Provides a request for the simulation configurations of all agv types.
/// </summary>
/// <param name="CorrelationId">The correlation id of the request.</param>
public record RequestAgvTypeConfigurations(Guid CorrelationId) : CorrelatedBy<Guid>;