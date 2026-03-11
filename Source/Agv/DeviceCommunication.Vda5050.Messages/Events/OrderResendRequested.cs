using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;

/// <summary>
/// Represent the event raised when resending the order to the agv has been requested by the fleet manager.
/// </summary>
/// <param name="CorrelationId">The correlation id of the order.</param>
public record OrderResendRequested(Guid CorrelationId) : CorrelatedBy<Guid>;