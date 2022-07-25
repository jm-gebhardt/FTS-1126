using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Order State Machine to orchestrate AGV transport orders 
    ///
    /// This is implemented as a MassTransit StateMachine <see href="https://masstransit-project.com/usage/sagas/automatonymous.html"/>
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public sealed class OrderStateMachine : MassTransitStateMachine<OrderSaga>
    {
        /// <summary>
        /// State when Order has been received
        /// </summary>
        public State OrderReceived { get; private set; } = default!;

        /// <summary>
        /// State when HandleHostOrder has been received and AssignAgv has been published
        /// </summary>
        public State AgvRequested { get; private set; } = default!;

        /// <summary>
        /// State when the AgvAssignmentSucceeded has been received and ExecuteTransportOrder has been published
        /// </summary>
        public State TransportOrderSend { get; private set; } = default!;

        /// <summary>
        /// State when the TransportOrderFailed has been received
        /// </summary>
        public State TransportOrderFailed { get; private set; } = default!;

        /// <summary>
        /// HandleHostOrder was received. This is the Initial Event of the OrderStateMachine
        /// </summary>
        public Event<HandleHostOrder> HandleHostOrderEvent { get; private set; } = default!;

        /// <summary>
        /// AgvAssignmentSucceeded was received
        /// </summary>
        public Event<RequestOrderAgvAssign> RequestOrderAgvAssignEvent { get; private set; } = default!;

        /// <summary>
        /// AgvAssignmentSucceeded was received
        /// </summary>
        public Event<AgvAssignmentSucceeded> AgvAssignSucceededEvent { get; private set; } = default!;

        /// <summary>
        /// AgvAssignmentFailed was received
        /// </summary>
        public Event<AgvAssignmentFailed> AgvAssignFailedEvent { get; private set; } = default!;

        /// <summary>
        /// TransportOrderCompleted was received. This is a final event
        /// </summary>
        public Event<TransportOrderCompleted> TransportOrderCompletedEvent { get; private set; } = default!;

        /// <summary>
        /// TransportOrderFailed was received. This is a final event
        /// </summary>
        public Event<TransportOrderFailed> TransportOrderFailedEvent { get; private set; } = default!;

        private readonly ILogger<OrderStateMachine> _logger;

        /// <summary>
        /// OrderStateMachine
        /// </summary>
        public OrderStateMachine(ILogger<OrderStateMachine> logger)
        {
            _logger = logger;

            SetSagaFactory();
            InstanceState(x => x.CurrentState);
            Initially(SetHandleHostOrderHandler());
            During(OrderReceived, SetRequestAgvAssignHandler());
            During(AgvRequested, SetAgvAssignSucceededHandler());
            During(AgvRequested, SetAgvAssignFailedHandler());
            During(TransportOrderSend, SetTransportOrderCompletedHandler());
            During(TransportOrderSend, SetTransportOrderFailedHandler());
        }

        /// <summary>
        /// Set saga factory on HandleHostOrderEvent
        /// </summary>
        private void SetSagaFactory()
        {
            // Initial creation
            Event(() => HandleHostOrderEvent, e =>
                e.SetSagaFactory(context => new OrderSaga(context.Message.CorrelationId, context.Message.Order))
            );
        }

        /// <summary>
        /// Handler for HandleHostOrder
        /// </summary>
        /// <returns>Event Binding for HandleHostOrder</returns>
        private EventActivityBinder<OrderSaga, HandleHostOrder> SetHandleHostOrderHandler() =>
            When(HandleHostOrderEvent)
                .TransitionTo(OrderReceived)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId}/{CorrelationId} received", c.Message.Order.OrderId, c.Saga.CorrelationId))
                .Publish(c => 
                    new RequestOrderAgvAssign(c.Saga.CorrelationId)
                );

        private EventActivityBinder<OrderSaga, RequestOrderAgvAssign> SetRequestAgvAssignHandler() =>
            When(RequestOrderAgvAssignEvent)
                .Publish(c => 
                    new AssignAgv(c.Saga.CorrelationId, c.Saga.Order)
                )
                .TransitionTo(AgvRequested);

        /// <summary>
        /// Handler for AgvAssignmentSucceeded
        /// </summary>
        /// <returns>Event Binding for AgvAssignmentSucceeded</returns>
        private EventActivityBinder<OrderSaga, AgvAssignmentSucceeded> SetAgvAssignSucceededHandler() =>
            When(AgvAssignSucceededEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId}/{CorrelationId} has been assigned to AGV {AgvId}",
                        c.Saga.Order.OrderId, c.Saga.CorrelationId, c.Message.AgvId)
                 )
                .Then(UpdateSagaState)
                .Publish(c =>
                    new ExecuteTransportOrder(c.Saga.CorrelationId, c.Saga.Order, c.Message.AgvId, c.Message.AgvCorrelationId)
                )
                .Publish(c =>
                    new HostOrderStarted(c.Saga.CorrelationId, c.Saga.Order.OrderId, c.Message.AgvId)
                )
                .TransitionTo(TransportOrderSend);

        /// <summary>
        /// Handler for AgvAssignmentFailed
        /// </summary>
        /// <returns>Event Binding for AgvAssignmentFailed</returns>
        private EventActivityBinder<OrderSaga, AgvAssignmentFailed> SetAgvAssignFailedHandler() =>
            When(AgvAssignFailedEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId}/{CorrelationId}, AGV assignment failed",
                        c.Saga.Order.OrderId, c.Saga.CorrelationId)
                )
                .TransitionTo(OrderReceived);

        /// <summary>
        /// Handler for AgvAssignmentSucceeded
        /// </summary>
        /// <returns>Event Binding for AgvAssignmentSucceeded</returns>
        private EventActivityBinder<OrderSaga, TransportOrderCompleted> SetTransportOrderCompletedHandler() =>
            When(TransportOrderCompletedEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId}/{CorrelationId} has been completed", c.Saga.Order.OrderId, c.Saga.CorrelationId)
                 )
                .ThenAsync(SendReleaseAgv)
                .ThenAsync(SendHostOrderCompleted)
                .Finalize();

        private void UpdateSagaState(BehaviorContext<OrderSaga, AgvAssignmentSucceeded> context)
        {
            context.Saga.AssignedAgv = context.Message.AgvCorrelationId;
        }

        private async Task SendHostOrderCompleted(BehaviorContext<OrderSaga, TransportOrderCompleted> context)
        {
            await context.Publish(new HostOrderHandlingSucceeded(context.Saga.CorrelationId, context.Saga.Order.OrderId));
        }

        private async Task SendReleaseAgv(BehaviorContext<OrderSaga, TransportOrderCompleted> context)
        {
            if (context.Saga.AssignedAgv != null)
            {
                await context.Publish(new ReleaseAgv(context.Saga.CorrelationId));
            }
        }

        /// <summary>
        /// Handler for TransportOrderFailed
        /// </summary>
        /// <returns>Event Binding for TransportOrderFailed</returns>
        private EventActivityBinder<OrderSaga, TransportOrderFailed> SetTransportOrderFailedHandler() =>
            When(TransportOrderFailedEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId}/{CorrelationId} has failed", c.Saga.Order.OrderId, c.Saga.CorrelationId)
                 )
                .TransitionTo(TransportOrderFailed);
    }
}
