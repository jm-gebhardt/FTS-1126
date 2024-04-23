using Gebhardt.Acc.Common.Messages;
using MassTransit;

namespace Gebhardt.Acc.WCSCommunication.Messages.Commands;

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