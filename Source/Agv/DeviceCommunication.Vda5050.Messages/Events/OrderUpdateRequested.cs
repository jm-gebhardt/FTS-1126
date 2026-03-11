using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;

/// <summary>
/// Represents the event raised, when an order update has been requested by the fleet manager.
/// </summary>
/// <param name="CorrelationId">The correlation id of the order to update.</param>
/// <param name="Order">The new order details.</param>
/// <param name="CurrentFleetManagerActionId">The current fleet manager action id.</param>
public record OrderUpdateRequested(Guid CorrelationId, Order Order, Guid? CurrentFleetManagerActionId) : CorrelatedBy<Guid>;