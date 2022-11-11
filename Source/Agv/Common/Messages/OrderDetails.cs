using System.Text.Json.Serialization;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages
{
    /// <summary>
    /// Order object used by all messages that require the order details
    /// </summary>
    [Serializable]
    public record OrderDetails : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// The unique ID of the order known by the host system
        /// </summary>
        public string OrderId { get; }

        /// <summary>
        /// The source where to pick up the order
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// The destination where to drop the order
        /// </summary>
        public string Destination { get; }

        /// <summary>
        /// The LeNo of the orders handled box or transportation good
        /// </summary>
        public string LeNo { get; }

        /// <summary>
        /// The creator of the host order
        /// </summary>
        public HostOrderCreator Creator { get; }

        /// <summary>
        /// The UTC timestamp when the order was created
        /// </summary>
        public DateTime CreationDate { get; }


        /// <summary>
        /// Initializes a new instance of <see cref="OrderDetails"/> with a specific creation date
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="orderId"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="leNo"></param>
        /// <param name="creator"></param>
        /// <param name="creationDate"></param>
        [JsonConstructor]
        public OrderDetails(Guid correlationId, string orderId, string source, string destination, string leNo, HostOrderCreator creator, DateTime creationDate)
        {
            CorrelationId = correlationId;
            OrderId = orderId;
            Source = source;
            Destination = destination;
            LeNo = leNo;
            Creator = creator;
            CreationDate = creationDate;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="OrderDetails"/> with creation date 'now'
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="orderId"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="leNo"></param>
        /// <param name="creator"></param>
        public OrderDetails(Guid correlationId, string orderId, string source, string destination, string leNo, HostOrderCreator creator)
            : this(correlationId, orderId, source, destination, leNo, creator, DateTime.UtcNow)
        {
        }
    }

    /// <summary>
    /// Defines the creator of a host order 
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public enum HostOrderCreator
    {
        /// <summary>
        /// A Warehouse Management System created the order (e.g. StoreWare)
        /// </summary>
        Wms,
        /// <summary>
        /// A simulation module is creator of the order
        /// </summary>
        Simulation
    }
}
