using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to stop the agv from being considered for the queue.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record RemoveAgvFromQueueConsideration(Guid CorrelationId) : CorrelatedBy<Guid>;