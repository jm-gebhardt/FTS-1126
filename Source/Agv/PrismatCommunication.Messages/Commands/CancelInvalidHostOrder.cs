using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands;

/// <summary>
/// Internal Message to signal canceling of a host order that was validated externally and reported to be invalid.
/// </summary>
/// <param name="CorrelationId">The correlation id of the host order.</param>
public record CancelInvalidHostOrder(Guid CorrelationId): CorrelatedBy<Guid>;