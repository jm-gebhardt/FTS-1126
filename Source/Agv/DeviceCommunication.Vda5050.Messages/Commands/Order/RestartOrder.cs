using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Order;

/// <summary>
/// Provides a command to restart an order.
/// </summary>
/// <param name="CorrelationId">The correlation id of the order.</param>
public record RestartOrder(Guid CorrelationId) : CorrelatedBy<Guid>;