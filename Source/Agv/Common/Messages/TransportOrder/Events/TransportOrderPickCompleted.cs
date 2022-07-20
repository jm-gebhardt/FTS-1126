using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events
{
    /// <summary>
    /// An event that reports the successful partial completion of an order. The transport good was picked at the source station.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record TransportOrderPickCompleted : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// The unique ID of the order known by the host system
        /// </summary>
        public string OrderId { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="TransportOrderPickCompleted"/>
        /// </summary>
        public TransportOrderPickCompleted(Guid correlationId, string orderId)
        {
            CorrelationId = correlationId;
            OrderId = orderId;
        }
    }
}
