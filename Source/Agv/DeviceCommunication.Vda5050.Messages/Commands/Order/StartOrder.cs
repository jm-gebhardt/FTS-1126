using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Order;

/// <summary>
/// Provides a command to start an order.
/// </summary>
[PublicAPI]
public record StartOrder: CorrelatedBy<Guid>
{
    /// <summary>
    /// The correlation id of the order.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// The order to be started.
    /// </summary>
    public Domain.Models.Orders.Order Order { get; init; }

    /// <inheritdoc cref="StartOrder"/>
    public StartOrder(Guid correlationId, Domain.Models.Orders.Order order)
    {
        CorrelationId = correlationId;
        Order = order;
    }
}