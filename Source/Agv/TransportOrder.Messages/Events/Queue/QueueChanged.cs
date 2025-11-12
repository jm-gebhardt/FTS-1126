using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Queue;

/// <summary>
/// Represents a change in the queue. More specifically, a change in the reservation or occupancy of its queue nodes.
/// </summary>
/// <param name="Queue">The queue that changed.</param>
public record QueueChanged(Common.Messages.Queue.Queue Queue) : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId => Queue.CorrelationId;
}