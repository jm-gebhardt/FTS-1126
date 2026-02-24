using MassTransit;

namespace Gebhardt.Acc.Common.Messages.HostOrder.Events;

/// <summary>
/// Represents the event raised when a host order has been created.
/// </summary>
public record HostOrderCreated : CorrelatedBy<Guid>
{
    /// <summary>
    /// Correlation id of the order.
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Assigned AGV that needs to execute the HostOrder.
    /// </summary>
    public Guid? AssignedAgv { get; }

    /// <summary>
    /// OrderID
    /// </summary>
    public OrderDetails OrderDetails { get; }

    /// <summary>
    /// Handling unit
    /// </summary>
    public string Hu { get; }

    /// <inheritdoc cref="HostOrderCreated"/>
    public HostOrderCreated(string hu, OrderDetails orderDetails, Guid? assignedAgv = null)
    {
        CorrelationId = orderDetails.CorrelationId;
        OrderDetails = orderDetails;
        AssignedAgv = assignedAgv;
        Hu = hu;
    }
}