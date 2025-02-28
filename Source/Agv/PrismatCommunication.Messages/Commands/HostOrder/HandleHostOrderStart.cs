using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;

/// <summary>
/// Provides a command to handle the start of a host order.
/// </summary>
/// <param name="CorrelationId">The correlation id of the order.</param>
/// <param name="AssignedAgv">The assigned agv</param>
/// <param name="Timestamp">The time the order was started.</param>
public record HandleHostOrderStart(Guid CorrelationId, Guid AssignedAgv, DateTime Timestamp) : CorrelatedBy<Guid>;