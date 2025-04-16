using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;

/// <summary>
/// Provides a command to report successful handling of an order.
/// </summary>
/// <param name="CorrelationId">The correlation od of the order.</param>
/// <param name="Timestamp">The time the order was finished.</param>
/// <param name="TargetNode">The node where the order was finished.</param>
public record FinishHostOrder(Guid CorrelationId, DateTime Timestamp, string? TargetNode) : CorrelatedBy<Guid>;