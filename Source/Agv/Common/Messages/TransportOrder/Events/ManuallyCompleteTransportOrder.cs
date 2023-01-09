using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events
{
    /// <summary>
    /// An event that am order has been completed manually so it will be reported as finished
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record ManuallyCompleteTransportOrder : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ManuallyCompleteTransportOrder"/>
        /// </summary>
        public ManuallyCompleteTransportOrder(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
