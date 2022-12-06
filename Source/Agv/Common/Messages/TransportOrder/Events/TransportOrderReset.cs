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
        /// Initializes a new instance of <see cref="TransportOrderReset"/>
        /// </summary>
        public TransportOrderReset(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
