using Gebhardt.StoreWare.Agv.Common.Messages;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Messages;

/// <summary>
/// Internal message to signal new host order within WCSCommunication
/// </summary>
public record CreateHostOrder : CorrelatedBy<Guid>
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
    /// Constructor
    /// </summary>
    /// <param name="orderDetails">Order details</param>
    public CreateHostOrder(OrderDetails orderDetails)
    {
        CorrelationId = orderDetails.CorrelationId;
        OrderDetails = orderDetails;
    }
}