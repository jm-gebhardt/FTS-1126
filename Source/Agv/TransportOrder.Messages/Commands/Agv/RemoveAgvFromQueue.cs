using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to remove an agv from a queue.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="AlsoRemoveFromConsideration">Whether to also remove the agv from the buffer tracking the order of agvs near the queue.</param>
/// <param name="QueueId">If set, the Agv is only removed from this specific queue.</param>
/// <param name="QueueNodeId">If set, the Agv is only removed from this specific queue node.</param>
public record RemoveAgvFromQueue(Guid CorrelationId, bool AlsoRemoveFromConsideration = false, Guid? QueueId = null, Guid? QueueNodeId = null) : CorrelatedBy<Guid>;