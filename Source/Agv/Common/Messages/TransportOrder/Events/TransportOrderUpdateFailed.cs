using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events
{
    /// <summary>
    /// An event that reports the failed update of an order
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record TransportOrderUpdateFailed : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// The unique ID of the order known by the host system
        /// </summary>
        public string OrderId { get; }

        /// <summary>
        /// The error code that holds information why the update of the order failed
        /// </summary>
        public TransportOrderErrorCode Error { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="TransportOrderUpdateFailed"/>
        /// </summary>
        public TransportOrderUpdateFailed(Guid correlationId, string orderId, TransportOrderErrorCode error)
        {
            CorrelationId = correlationId;
            OrderId = orderId;
            Error = error;
        }
    }
}
