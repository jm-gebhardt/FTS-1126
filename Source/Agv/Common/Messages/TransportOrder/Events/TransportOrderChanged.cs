using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events
{
    /// <summary>
    /// Event that is raised when a transport order changed
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record TransportOrderChanged : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Current state of the order
        /// </summary>
        public IOrder Order { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="TransportOrderChanged"/>
        /// </summary>
        public TransportOrderChanged(Guid correlationId, IOrder order)
        {
            CorrelationId = correlationId;
            Order = order;
        }
    }
}
