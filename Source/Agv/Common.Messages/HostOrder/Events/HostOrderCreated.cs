using MassTransit;

namespace Gebhardt.Acc.Common.Messages.HostOrder.Events;

/// <summary>
/// Represents the event raised when a host order has been created.
/// </summary>
public record HostOrderCreated : CorrelatedBy<Guid>
{
    /// <summary>
    /// Correlated by OrderId
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// OrderID
    /// </summary>
    public OrderDetails OrderDetails { get; }

    /// <summary>
    /// Handling unit
    /// </summary>
    public string Hu { get; }

    /// <inheritdoc cref="HostOrderCreated"/>
    public HostOrderCreated(string hu, OrderDetails orderDetails)
    {
        CorrelationId = orderDetails.CorrelationId;
        OrderDetails = orderDetails;
        Hu = hu;
    }
}