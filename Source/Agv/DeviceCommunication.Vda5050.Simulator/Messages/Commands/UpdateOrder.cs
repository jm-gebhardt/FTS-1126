using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;

/// <summary>
/// Provides a command to update an order.
/// </summary>
/// <param name="CorrelationId">the correlation if the agv.</param>
/// <param name="Order">The order.</param>
public record UpdateOrder(Guid CorrelationId, Order Order) : CorrelatedBy<Guid>;