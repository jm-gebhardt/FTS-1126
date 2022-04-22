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
        public State CurrentState { get; set; } = default!;

        /// <summary>
        /// CorrelationId
        /// <see href="https://masstransit-project.com//usage/correlation.html" />
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        ///  Order ID
        /// </summary>
        public string OrderId { get; set; } = default!;

        /// <summary>
        /// Source
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? Destination { get; set; }

        /// <summary>
        /// Destination
        /// </summary>
        public string? AssignedAgv { get; set; }
    }
}