using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to start the agv being considered for the queue.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="QueueId">The id of the queue.</param>
public record AddAgvToQueueConsideration(Guid CorrelationId, Guid QueueId) : CorrelatedBy<Guid>;