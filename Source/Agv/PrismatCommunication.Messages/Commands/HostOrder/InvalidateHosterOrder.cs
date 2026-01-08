using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;

/// <summary>
/// Provides a command to invalidate a host order.
/// </summary>
/// <param name="CorrelationId">The correlation id of the order.</param>
/// <param name="Error">The validation error.</param>
public record InvalidateHosterOrder(Guid CorrelationId, ValidationError Error): CorrelatedBy<Guid>;