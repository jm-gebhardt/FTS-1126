using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.States
{

    /// <summary>
    /// Order Saga storing the current state of an Order
    /// <see href="https://masstransit-project.com/usage/sagas/automatonymous.html#instance" />
    /// </summary>
    [PublicAPI]
    public class OrderSaga :
        SagaStateMachineInstance, IOrder
    {
        /// <inheritdoc/>
        public string CurrentState { get; set; } = default!;

        /// <inheritdoc cref="CorrelatedBy&lt;T>" />
        public Guid CorrelationId { get; set; }

        /// <inheritdoc/>
        public Guid? AssignedAgv { get; set; }

        /// <inheritdoc/>
        public OrderDetails OrderDetails { get; set; } = null!;

        /// <inheritdoc/>
        public string? FailedReason { get; set; }

        /// <summary>
        /// Create new OrderSaga
        /// </summary>
        /// <param name="orderDetails">The order related to this Saga</param>
        /// <param name="correlationId">If the correlationId is empty, a new Id will be generated</param>
        public OrderSaga(Guid correlationId, OrderDetails orderDetails)
        {
            if (correlationId == Guid.Empty)
            {
                throw new InvalidOperationException($"{this.GetType()} requires correlation ID.");
            }
            CorrelationId = correlationId;
            OrderDetails = orderDetails;
        }

        /// <summary>
        /// OrderSaga constructor used to restore an instance from persistence
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="orderDetails"></param>
        /// <param name="currentState"></param>
        /// <param name="assignedAgv"></param>
        [System.Text.Json.Serialization.JsonConstructor]
        public OrderSaga(Guid correlationId, OrderDetails orderDetails, string currentState, Guid? assignedAgv): 
            this(correlationId, orderDetails)
        {
            CurrentState = currentState;
            AssignedAgv = assignedAgv;
        }

        // Private constructor for Entity Framework 
        private OrderSaga(Guid correlationId, string currentState, Guid? assignedAgv)
        {
            CorrelationId = correlationId;
            CurrentState = currentState;
            AssignedAgv = assignedAgv;
        } 
    }
}