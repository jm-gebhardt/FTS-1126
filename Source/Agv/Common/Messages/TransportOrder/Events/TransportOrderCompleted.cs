using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events
{
    /// <summary>
    /// An event that reports the successful completion of an order
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record TransportOrderCompleted : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// The unique ID of the order known by the host system
        /// </summary>
        public string OrderId { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="TransportOrderCompleted"/>
        /// </summary>
        public TransportOrderCompleted(Guid correlationId, string orderId)
        {
            CorrelationId = correlationId;
            OrderId = orderId;
        }
    }
}
