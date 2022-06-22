using Gebhardt.StoreWare.Agv.Common.Messages;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.States
{

    /// <summary>
    /// Order Saga storing the current state of an Order
    /// <see href="https://masstransit-project.com/usage/sagas/automatonymous.html#instance" />
    /// </summary>
    [PublicAPI]
    public class OrderSaga :
        SagaStateMachineInstance, CorrelatedBy<Guid>
    {

        /// <summary>
        /// Current State
        /// </summary>
        public string CurrentState { get; set; } = default!;

        /// <summary>
        /// CorrelationId
        /// <see href="https://masstransit-project.com//usage/correlation.html" />
        /// </summary>
        public Guid CorrelationId { get; set; }
        
        /// <summary>
        /// AssignedAgv
        /// </summary>
        public string? AssignedAgv { get; set; }

        /// <summary>
        /// The order details of this saga
        /// </summary>
        public Order Order { get; set; } = null!;

        /// <summary>
        /// Create new OrderSaga
        /// </summary>
        /// <param name="order">The order related to this Saga</param>
        /// <param name="correlationId">If the correlationId is empty, a new Id will be generated</param>
        public OrderSaga(Guid correlationId, Order order)
        {
            if (correlationId == Guid.Empty)
            {
                throw new InvalidOperationException($"{this.GetType()} requires correlation ID.");
            }
            CorrelationId = correlationId;
            Order = order;
        }

        /// <summary>
        /// OrderSaga constructor used to restore an instance from persistence
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="order"></param>
        /// <param name="currentState"></param>
        /// <param name="assignedAgv"></param>
        public OrderSaga(Guid correlationId, Order order, string currentState, string? assignedAgv): 
            this(correlationId, order)
        {
            CurrentState = currentState;
            AssignedAgv = assignedAgv;
        }

        // Private constructor for Entity Framework 
        private OrderSaga(Guid correlationId, string currentState, string? assignedAgv)
        {
            CorrelationId = correlationId;
            CurrentState = currentState;
            AssignedAgv = assignedAgv;
        } 
    }
}