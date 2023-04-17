
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events
{
    /// <summary>
    /// An event that reports a failed cancel of an order
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record HostOrderCancelFailed : CorrelatedBy<Guid>
    {
        /// <summary>
        /// Error code that can occur during cancellation of host orders
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public enum ErrorCode
        {
            /// <summary>
            /// Order does not exist
            /// </summary>
            OrderDoesNotExist,

            /// <summary>
            /// Order has already been started
            /// </summary>
            OrderAlreadyStarted,

            /// <summary>
            /// Order already finished
            /// </summary>
            OrderAlreadyFinished,

            /// <summary>
            /// Order already finished with failure
            /// </summary>
            OrderAlreadyFailed,

            /// <summary>
            /// Order already cancelled
            /// </summary>
            OrderAlreadyCancelled
        }

        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// The unique ID of the order known by the host system
        /// </summary>
        public string OrderId { get; }

        /// <summary>
        /// The error code that holds information why cancellation of the order failed
        /// </summary>
        public ErrorCode Error { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="HostOrderCancelFailed"/>
        /// </summary>
        public HostOrderCancelFailed(Guid correlationId, string orderId, ErrorCode error)
        {
            CorrelationId = correlationId;
            OrderId = orderId;
            Error = error;
        }
    }
}
