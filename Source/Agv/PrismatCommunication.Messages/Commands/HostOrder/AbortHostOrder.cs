using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;

/// <summary>
/// Provides a command to abort a host order.
/// </summary>
/// <param name="CorrelationId">The correlation id of the host order.</param>
public record AbortHostOrder(Guid CorrelationId): CorrelatedBy<Guid>;