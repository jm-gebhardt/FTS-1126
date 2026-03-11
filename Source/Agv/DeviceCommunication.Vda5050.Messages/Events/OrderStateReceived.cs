using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;

/// <summary>
/// Represents the event raised, when a new order state has been received.
/// </summary>
/// <param name="CorrelationId">The correlation id of the order.</param>
/// <param name="OrderState">The oder state.</param>
public record OrderStateReceived(Guid CorrelationId, OrderState OrderState) : CorrelatedBy<Guid>;
