using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Represents the event raised when an action has been completed.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
public record NonChargingActionCompleted(Guid CorrelationId) : CorrelatedBy<Guid>;