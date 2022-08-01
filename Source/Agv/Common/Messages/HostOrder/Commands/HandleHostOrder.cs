using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands
{
    /// <summary>
    /// A command from the host system to create a transport order
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record HandleHostOrder : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Order Details
        /// </summary>
        public OrderDetails OrderDetails { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="HandleHostOrder"/>
        /// </summary>
        public HandleHostOrder(Guid correlationId, OrderDetails orderDetails)
        {
            CorrelationId = correlationId;
            OrderDetails = orderDetails;
        }
    }
}

