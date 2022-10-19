using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Contracts;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands
{
    /// <summary>
    /// A command from the transport order module to the agv communication module to execute an order
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record ExecuteTransportOrder : CorrelatedBy<Guid>, IAgvId
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Order Details
        /// </summary>
        public OrderDetails OrderDetails { get; }

        /// <inheritdoc />
        public string AgvId { get; }

        /// <summary>
        /// List of node ids that define the route
        /// </summary>
        public string[] Nodes { get; set; }
        
        /// <summary>
        /// Initializes a new instance of <see cref="ExecuteTransportOrder"/>
        /// </summary>
        public ExecuteTransportOrder(Guid correlationId, OrderDetails orderDetails, string agvId, string[] nodes)
        {
            CorrelationId = correlationId;
            OrderDetails = orderDetails;
            AgvId = agvId;
            Nodes = nodes;
        }
    }
}
