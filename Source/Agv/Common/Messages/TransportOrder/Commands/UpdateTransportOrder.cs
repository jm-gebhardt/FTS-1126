using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands
{
    /// <summary>
    /// A command from the transport order module to the agv communication module to update an order
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record UpdateTransportOrder : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Order Details
        /// </summary>
        public Order Order { get; }

        /// <summary>
        /// The assigned AGV
        /// </summary>
        public string AssignedAgv { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="UpdateTransportOrder"/>
        /// </summary>
        public UpdateTransportOrder(Guid correlationId, Order order, string assignedAgv)
        {
            CorrelationId = correlationId;
            Order = order;
            AssignedAgv = assignedAgv;
        }
    }
}
