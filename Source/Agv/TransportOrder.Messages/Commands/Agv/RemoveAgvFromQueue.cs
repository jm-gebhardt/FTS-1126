using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to remove an agv from a queue.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record RemoveAgvFromQueue(Guid CorrelationId): CorrelatedBy<Guid>;