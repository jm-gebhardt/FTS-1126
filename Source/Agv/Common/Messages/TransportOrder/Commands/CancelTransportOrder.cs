using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands
{
    /// <summary>
    /// A command from the transport order module to the agv communication module to cancel an order
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record CancelTransportOrder : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// The unique ID of the order known by the host system
        /// </summary>
        public string OrderId { get; }

        /// <summary>
        /// The agv that was assigned for this transport by the transport order module
        /// </summary>
        public string AssignedAgv { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="CancelTransportOrder"/>
        /// </summary>
        public CancelTransportOrder(Guid correlationId, string orderId, string assignedAgv)
        {
            CorrelationId = correlationId;
            OrderId = orderId;
            AssignedAgv = assignedAgv;
        }
    }
}
