using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the event raised when a queue node was selected for an agv.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="QueueNode">The queue node.</param>
public record QueueNodeSelectedForAgv(Guid CorrelationId, string QueueNode) : CorrelatedBy<Guid>;