using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events
{
    /// <summary>
    /// An event that reports the successful start of execution of an order
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record TransportOrderStarted : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// The unique ID of the order known by the host system
        /// </summary>
        public string OrderId { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="TransportOrderStarted"/>
        /// </summary>
        public TransportOrderStarted(Guid correlationId, string orderId)
        {
            CorrelationId = correlationId;
            OrderId = orderId;
        }
    }
}
