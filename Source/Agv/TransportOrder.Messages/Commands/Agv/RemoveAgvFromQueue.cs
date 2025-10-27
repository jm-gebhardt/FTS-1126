using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to remove an agv from a queue.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="AlsoRemoveFromConsideration">Whether to also remove the agv from the buffer that is tracking the order of agvs near the queue.</param>
public record RemoveAgvFromQueue(Guid CorrelationId, bool AlsoRemoveFromConsideration = false): CorrelatedBy<Guid>;