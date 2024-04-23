using MassTransit;

namespace Gebhardt.Acc.WCSCommunication.Messages.Commands;

/// <summary>
/// Provides a command to report successful handling of an order.
/// </summary>
/// <param name="CorrelationId">The correlation od of the order.</param>
/// <param name="Timestamp">The time the order was finished.</param>
public record FinishHostOrder(Guid CorrelationId, DateTime Timestamp) : CorrelatedBy<Guid>;