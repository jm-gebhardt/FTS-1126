using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to remove queue blocking warning, if possible.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record TryRemoveQueueBlocking(Guid CorrelationId): CorrelatedBy<Guid>;