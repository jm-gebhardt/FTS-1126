using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events
{
    /// <summary>
    /// An event that reports the failed execution of an order
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record TransportOrderReset : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// The error code that holds information why the update of the order failed
        /// </summary>
        public TransportOrderErrorCode Error { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="TransportOrderReset"/>
        /// </summary>
        public TransportOrderReset(Guid correlationId, TransportOrderErrorCode error)
        {
            CorrelationId = correlationId;
            Error = error;
        }
    }
}
