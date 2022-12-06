using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events
{
    /// <summary>
    /// An event that reports a failure while handling an order
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record HostOrderHandlingFailed : CorrelatedBy<Guid>
    {
        /// <summary>
        /// Error code that can occur during handling of host orders
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public enum ErrorCode
        {
            /// <summary>
            /// Order already exists
            /// </summary>
            OrderAlreadyExists,

            /// <summary>
            /// Order data is invalid
            /// </summary>
            InvalidOrderData,

            /// <summary>
            /// Order failed by transport layer (e.g. AGV)
            /// </summary>
            FailedByTransportLayer
        }


        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// The unique ID of the order known by the host system
        /// </summary>
        public string OrderId { get; }

        /// <summary>
        /// The error code that holds information why the handling of the order failed
        /// </summary>
        public ErrorCode Error { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="HostOrderHandlingFailed"/>
        /// </summary>
        public HostOrderHandlingFailed(Guid correlationId, string orderId, ErrorCode error)
        {
            CorrelationId = correlationId;
            OrderId = orderId;
            Error = error;
        }
    }
}
