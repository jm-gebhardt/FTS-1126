using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.States
{
    /// <summary>
    /// Represents an host order as a state machine saga.
    /// </summary>
    public class HostOrderSaga : SagaStateMachineInstance
    {
        /// <summary>
        /// Correlated by GUID of OrderId
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Redundant OrderId info
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// Current state
        /// </summary>
        [JetBrains.Annotations.UsedImplicitly]
        public string CurrentState { get; set; }

        /// <summary>
        /// Order details
        /// </summary>
        public OrderDetails OrderDetails { get; set; } = null!;

        /// <summary>
        /// Pick Action's GUID
        /// </summary>
        public Guid PickActionGuid { get; } = Guid.NewGuid();

        /// <summary>
        /// Drop Action's GUID
        /// </summary>
        public Guid DropActionGuid { get; } = Guid.NewGuid();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="orderDetails">Order details</param>
        /// <param name="currentState">Order state</param>
        public HostOrderSaga(OrderDetails orderDetails, string? currentState = null)
        {
            CorrelationId = orderDetails.CorrelationId;
            OrderId = orderDetails.OrderId;
            CurrentState = currentState ?? default!;
            OrderDetails = orderDetails;
        }

        /// <summary>
        /// Private constructor for Entity Framework
        /// </summary>
        /// <param name="correlationId">Correlated by GUID of OrderId</param>
        /// <param name="orderId">Redundant OrderId info</param>
        /// <param name="currentState">Order state</param>
        /// <param name="pickActionGuid">Pick Action GUID</param>
        /// <param name="dropActionGuid">Drop Action GUID</param>
        private HostOrderSaga(Guid correlationId, string orderId, string currentState, Guid pickActionGuid, Guid dropActionGuid)
        {
            CorrelationId = correlationId;
            OrderId = orderId;
            CurrentState = currentState;
            PickActionGuid = pickActionGuid;
            DropActionGuid = dropActionGuid;
        }

        /// <summary>
        /// Private constructor for Entity Framework used to restore an instance from persistence
        /// </summary>
        /// <param name="correlationId">Correlated by GUID of OrderId</param>
        /// <param name="orderId">Redundant OrderId info</param>
        /// <param name="currentState">Order state</param>
        /// <param name="orderDetails">Order details</param>
        [System.Text.Json.Serialization.JsonConstructor]
        private HostOrderSaga(Guid correlationId, string orderId, string currentState, OrderDetails orderDetails)
        {
            CorrelationId = correlationId;
            OrderId = orderId;
            CurrentState = currentState;
            OrderDetails = orderDetails;
        }
    }
}
