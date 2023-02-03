using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Messages
{
    /// <summary>
    /// Event when an order has been assigned to an AGV
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class OrderAssignmentSucceeded : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// The AGV Id 
        /// </summary>
        public string AgvId { get; }

        /// <summary>
        /// The AGV correlation Id
        /// </summary>
        public Guid AgvCorrelationId { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="OrderAssignmentSucceeded"/>
        /// </summary>
        public OrderAssignmentSucceeded(Guid correlationId, string agvId, Guid agvCorrelationId)
        {
            CorrelationId = correlationId;
            AgvId = agvId;
            AgvCorrelationId = agvCorrelationId;
        }
    }
}
