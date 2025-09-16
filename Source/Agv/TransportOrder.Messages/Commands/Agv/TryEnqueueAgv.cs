using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to try to enqueue an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="EntryNodeId">The entry node of the queue.</param>
public record TryEnqueueAgv(Guid CorrelationId, Guid EntryNodeId): CorrelatedBy<Guid>;