using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to leave a queue.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record LeaveQueue(Guid CorrelationId): CorrelatedBy<Guid>;