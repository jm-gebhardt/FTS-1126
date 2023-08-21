using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;

/// <summary>
/// A command from the host system to update an existing transport order
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record UpdateHostOrder : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Updated order details
    /// </summary>
    public OrderDetails OrderDetails { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="UpdateHostOrder"/>
    /// </summary>
    public UpdateHostOrder(Guid correlationId, OrderDetails orderDetails)
    {
        CorrelationId = correlationId;
        OrderDetails = orderDetails;
    }
}