using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;

/// <summary>
/// Represent the event raised when the fleet manager requests to cancel an order.
/// </summary>
/// <param name="CorrelationId">The correlation id of the order.</param>
public record OrderCancellationRequested(Guid CorrelationId) : CorrelatedBy<Guid>;