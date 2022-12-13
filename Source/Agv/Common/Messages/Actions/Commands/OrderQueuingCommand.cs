using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;


/// <summary>
/// Splits the TransportOrder into separate actions and places them in the queue
/// </summary>
public record OrderQueuingCommand : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    public Guid CorrelationId { get; }

    /// <summary>
    /// The details of the TransportOrder to be split
    /// </summary>
    public OrderDetails OrderDetails { get; }

    /// <summary>
    /// <inheritdoc cref="OrderQueuingCommand"/>
    /// </summary>
    /// <param name="correlationId">The correlationId</param>
    /// <param name="orderDetails">The details of the TransportOrder to be split</param>
    public OrderQueuingCommand(Guid correlationId,
        OrderDetails orderDetails)
    {
        CorrelationId = correlationId;
        OrderDetails = orderDetails;
    }
}