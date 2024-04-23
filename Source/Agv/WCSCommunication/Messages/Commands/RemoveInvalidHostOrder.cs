using MassTransit;

namespace Gebhardt.Acc.WCSCommunication.Messages.Commands;

/// <summary>
/// Provides a command to remove an invalid host order.
/// </summary>
/// <param name="CorrelationId">The correlation id of the host order.</param>
public record RemoveInvalidHostOrder(Guid CorrelationId) : CorrelatedBy<Guid>;