using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;

/// <summary>
/// Provides a request to remove a load from an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="LoadId">The id of the load to remove.</param>
public record RequestRemoveLoad(Guid CorrelationId, string LoadId) : CorrelatedBy<Guid>;