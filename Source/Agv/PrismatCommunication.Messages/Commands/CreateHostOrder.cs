using Gebhardt.Acc.Common.Messages;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands;

/// <summary>
/// Internal message to signal new host order within PrismatCommunication
/// </summary>
[UsedImplicitly]
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
    /// Handling unit
    /// </summary>
    public string Hu { get; }
    
    /// <inheritdoc cref="CreateHostOrder"/>
    public CreateHostOrder(string hu, OrderDetails orderDetails)
    {
        CorrelationId = orderDetails.CorrelationId;
        OrderDetails = orderDetails;
        Hu = hu;
    }
}