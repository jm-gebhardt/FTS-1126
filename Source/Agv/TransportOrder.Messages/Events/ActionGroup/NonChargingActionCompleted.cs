using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Represents the event raised when an action has been completed.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="IsQueueing">True, if the agv is queueing after action completion.</param>
public record NonChargingActionCompleted(Guid CorrelationId, bool IsQueueing=false) : CorrelatedBy<Guid>;