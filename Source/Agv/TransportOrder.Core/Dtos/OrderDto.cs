using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos
{
    /// <summary>
    /// Order Model
    ///
    /// This is a simply copy of the <see cref="IOrder"/> to overcome a json serialization problems
    /// <see href="https://github.com/dotnet/runtime/issues/41749" />
    /// </summary>
    public record OrderDto : IOrder
    {
        /// <inheritdoc/>
        public string CurrentState { get; }

        /// <inheritdoc/>
        public Guid? AssignedAgv { get; }

        /// <inheritdoc/>
        public OrderDetails OrderDetails { get; }

        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <inheritdoc/>
        public string? FailedReason { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="OrderDto"/>
        /// </summary>
        /// <param name="currentState">Current state of the order</param>
        /// <param name="assignedAgv">CorrelationId of the assigned AGV or null if order is not assigned to an AGV yet</param>
        /// <param name="orderDetails">Details about this order</param>
        /// <param name="correlationId">CorrelationId of this order</param>
        /// <param name="failedReason">Error string containing the reason why the order failed or null</param>
        [JetBrains.Annotations.PublicAPI]
        public OrderDto(string currentState, Guid? assignedAgv, OrderDetails orderDetails, Guid correlationId, string? failedReason = null)
        {
            CurrentState = currentState;
            AssignedAgv = assignedAgv;
            OrderDetails = orderDetails;
            CorrelationId = correlationId;
            FailedReason = failedReason;
        }
    }
}
