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
        /// 
        /// </summary>
        public State OrderReceived { get; private set; } = default!;

        /// <summary>
        /// State when IHandleHostOrder has been received and IAssignAgv has been published
        /// </summary>
        public State AgvRequested { get; private set; } = default!;

        /// <summary>
        /// State when the IAgvAssignmentSucceeded has been received and IExecuteTransportOrder has been published
        /// </summary>
        public State TransportOrderSend { get; private set; } = default!;

        /// <summary>
        /// State when the ITransportOrderFailed has been received
        /// </summary>
        public State TransportOrderFailed { get; private set; } = default!;

        /// <summary>
        /// IHandleHostOrder was received. This is the Initial Event of the OrderStateMachine
        /// </summary>
        public Event<IHandleHostOrder> HandleHostOrderEvent { get; private set; } = default!;

        /// <summary>
        /// IAgvAssignmentSucceeded was received
        /// </summary>
        public Event<RequestOrderAgvAssign> RequestOrderAgvAssignEvent { get; private set; } = default!;

        /// <summary>
        /// IAgvAssignmentSucceeded was received
        /// </summary>
        public Event<IAgvAssignmentSucceeded> AgvAssignSucceededEvent { get; private set; } = default!;

        /// <summary>
        /// AgvAssignmentFailed was received
        /// </summary>
        public Event<AgvAssignmentFailed> AgvAssignFailedEvent { get; private set; } = default!;

        /// <summary>
        /// ITransportOrderCompleted was received. This is a final event
        /// </summary>
        public Event<ITransportOrderCompleted> TransportOrderCompletedEvent { get; private set; } = default!;

        /// <summary>
        /// ITransportOrderFailed was received. This is a final event
        /// </summary>
        public Event<ITransportOrderFailed> TransportOrderFailedEvent { get; private set; } = default!;

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
        /// Handler for IHandleHostOrder
        /// </summary>
        /// <returns>Event Binding for IHandleHostOrder</returns>
        private EventActivityBinder<OrderSaga, IHandleHostOrder> SetHandleHostOrderHandler() =>
            When(HandleHostOrderEvent)
                .TransitionTo(OrderReceived)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId}/{CorrelationId} received", c.Message.Order.OrderId, c.Saga.CorrelationId))
                .Publish(c => new RequestOrderAgvAssign(c.Saga.CorrelationId));

        private EventActivityBinder<OrderSaga, RequestOrderAgvAssign> SetRequestAgvAssignHandler() =>
            When(RequestOrderAgvAssignEvent)
                .PublishAsync(c =>
                    c.Init<IAssignAgv>(new { c.Saga.CorrelationId, c.Saga.Order })
                )
                .TransitionTo(AgvRequested);

        /// <summary>
        /// Handler for IAgvAssignmentSucceeded
        /// </summary>
        /// <returns>Event Binding for IAgvAssignmentSucceeded</returns>
        private EventActivityBinder<OrderSaga, IAgvAssignmentSucceeded> SetAgvAssignSucceededHandler() =>
            When(AgvAssignSucceededEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId}/{CorrelationId} has been assigned to AGV {AgvId}",
                        c.Saga.Order.OrderId, c.Saga.CorrelationId, c.Message.AgvId)
                 )
                .Then(UpdateSagaState)
                .PublishAsync(c => c.Init<IExecuteTransportOrder>(
                    new { c.Saga.CorrelationId, c.Saga.Order, c.Message.AgvId, c.Message.AgvCorrelationId })
                )
                .PublishAsync(c =>
                    c.Init<IHostOrderStarted>(new { c.Saga.CorrelationId, c.Saga.Order.OrderId, AssignedAgv = c.Message.AgvId })
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
        /// Handler for IAgvAssignmentSucceeded
        /// </summary>
        /// <returns>Event Binding for IAgvAssignmentSucceeded</returns>
        private EventActivityBinder<OrderSaga, ITransportOrderCompleted> SetTransportOrderCompletedHandler() =>
            When(TransportOrderCompletedEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId}/{CorrelationId} has been completed", c.Saga.Order.OrderId, c.Saga.CorrelationId)
                 )
                .ThenAsync(SendReleaseAgv)
                .ThenAsync(SendHostOrderCompleted)
                .Finalize();

        private void UpdateSagaState(BehaviorContext<OrderSaga, IAgvAssignmentSucceeded> context)
        {
            context.Saga.AssignedAgv = context.Message.AgvCorrelationId;
        }

        private async Task SendHostOrderCompleted(BehaviorContext<OrderSaga, ITransportOrderCompleted> context)
        {
            await context.Publish<IHostOrderHandlingSucceeded>(new
            {
                context.Saga.CorrelationId,
                context.Saga.Order.OrderId
            });
        }

        private async Task SendReleaseAgv(BehaviorContext<OrderSaga, ITransportOrderCompleted> context)
        {
            if (context.Saga.AssignedAgv != null)
            {
                await context.Publish(new ReleaseAgv(context.Saga.CorrelationId));
            }
        }

        /// <summary>
        /// Handler for ITransportOrderFailed
        /// </summary>
        /// <returns>Event Binding for ITransportOrderFailed</returns>
        private EventActivityBinder<OrderSaga, ITransportOrderFailed> SetTransportOrderFailedHandler() =>
            When(TransportOrderFailedEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId}/{CorrelationId} has failed", c.Saga.Order.OrderId, c.Saga.CorrelationId)
                 )
                .TransitionTo(TransportOrderFailed);
    }
}
