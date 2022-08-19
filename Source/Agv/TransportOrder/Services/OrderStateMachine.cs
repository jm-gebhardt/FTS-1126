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
        /// State when the OrderFailed has been received
        /// </summary>
        public State OrderFailed { get; private set; } = default!;

        /// <summary>
        /// State when HandleHostOrder has been received and AssignAgv has been published
        /// </summary>
        public State AgvRequested { get; private set; } = default!;

        /// <summary>
        /// State when the AgvAssignmentSucceeded has been received and ExecuteTransportOrder has been published
        /// </summary>
        public State TransportOrderSend { get; private set; } = default!;

        /// <summary>
        /// State when TransportOrder has been started
        /// </summary>
        public State TransportOrderStarted { get; private set; } = default!;

        /// <summary>
        /// State when TransportOrder has been picked
        /// </summary>
        public State TransportOrderPicked { get; private set; } = default!;

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

        /// <summary>
        /// TransportOrderStarted has been received
        /// </summary>
        public Event<TransportOrderStarted> TransportOrderStartedEvent { get; private set; } = default!;

        /// <summary>
        /// TransportOrderPickCompleted has been received
        /// </summary>
        public Event<TransportOrderPickCompleted> TransportOrderPickCompletedEvent { get; private set; } = default!;

        private readonly ILogger<OrderStateMachine> _logger;

        private readonly IAgvRegistry _agvRegistry;

        /// <summary>
        /// OrderStateMachine
        /// </summary>
        public OrderStateMachine(ILogger<OrderStateMachine> logger, IAgvRegistry agvRegistry)
        {
            _logger = logger;
            _agvRegistry = agvRegistry;

            SetSagaFactory();
            InstanceState(x => x.CurrentState);
            Initially(SetHandleHostOrderHandler());
            During(OrderReceived, SetRequestAgvAssignHandler());
            During(AgvRequested, SetAgvAssignSucceededHandler());
            During(AgvRequested, SetAgvAssignFailedHandler());
            During(TransportOrderSend, TransportOrderPicked, SetTransportOrderFailedHandler());
            During(TransportOrderSend, SetTransportOrderStartedHandler());
            During(TransportOrderStarted, SetTransportOrderPickCompletedHandler());
            During(TransportOrderPicked, SetTransportOrderCompletedHandler());
        }

        /// <summary>
        /// Set saga factory on HandleHostOrderEvent
        /// </summary>
        private void SetSagaFactory()
        {
            // Initial creation
            Event(() => HandleHostOrderEvent, e =>
                e.SetSagaFactory(context => new OrderSaga(context.Message.CorrelationId, context.Message.OrderDetails))
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
                    _logger.LogInformation("Order {OrderId}/{CorrelationId} received", c.Message.OrderDetails.OrderId, c.Saga.CorrelationId))
                .Publish(c =>
                    new RequestOrderAgvAssign(c.Saga.CorrelationId)
                );

        /// <summary>
        /// Handler for RequestOrderAgvAssignS
        /// </summary>
        /// <returns>Event Binding for RequestOrderAgvAssign</returns>
        private EventActivityBinder<OrderSaga, RequestOrderAgvAssign> SetRequestAgvAssignHandler() =>
            When(RequestOrderAgvAssignEvent)
                .Publish(c =>
                    new AssignAgv(c.Saga.CorrelationId, c.Saga.OrderDetails)
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
                        c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId, c.Message.AgvId)
                 )
                .Then(UpdateSagaState)
                .Publish(c =>
                    new ExecuteTransportOrder(c.Message.AgvCorrelationId, c.Saga.OrderDetails, c.Message.AgvId, c.Message.Nodes)
                )
                .TransitionTo(TransportOrderSend);

        /// <summary>
        /// Handler for AgvAssignmentFailed
        /// </summary>
        /// <returns>Event Binding for AgvAssignmentFailed</returns>
        private EventActivityBinder<OrderSaga, AgvAssignmentFailed> SetAgvAssignFailedHandler() =>
            When(AgvAssignFailedEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId}/{CorrelationId}, AGV assignment failed {Error}",
                        c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId, c.Message.Error)
                )
                .Then(c => c.Saga.FailedReason =  $"{nameof(AgvAssignmentErrorCode)}.{c.Message.Error}")
                .IfElse(c => c.Message.Error == AgvAssignmentErrorCode.NoIdleAgv,
                    c => c
                        .TransitionTo(OrderReceived),
                    HostOrderInvalid
                );

        private EventActivityBinder<OrderSaga, AgvAssignmentFailed> HostOrderInvalid(EventActivityBinder<OrderSaga, AgvAssignmentFailed> context) =>
            context.Publish( c => new HostOrderHandlingFailed(c.Saga.CorrelationId, c.Saga.OrderDetails.OrderId, HostOrderErrorCode.InvalidOrderData))
                .Then(c => _logger.LogInformation("Order {OrderId}/{CorrelationId} contains invalid data.", c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId))
                .TransitionTo(OrderFailed);

        /// <summary>
        /// Handler for AgvAssignmentSucceeded
        /// </summary>
        /// <returns>Event Binding for AgvAssignmentSucceeded</returns>
        private EventActivityBinder<OrderSaga, TransportOrderCompleted> SetTransportOrderCompletedHandler() =>
            When(TransportOrderCompletedEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId}/{CorrelationId} has been completed", c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId)
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
            await context.Publish(new HostOrderHandlingSucceeded(context.Saga.CorrelationId, context.Saga.OrderDetails.OrderId));
        }

        private async Task SendReleaseAgv(BehaviorContext<OrderSaga, TransportOrderCompleted> context)
        {
            if (context.Saga.AssignedAgv != null)
            {
                await context.Publish(new ReleaseAgv(correlationId: context.Saga.AssignedAgv.Value));
            }
        }

        /// <summary>
        /// Handler for OrderFailed
        /// </summary>
        /// <returns>Event Binding for OrderFailed</returns>
        private EventActivityBinder<OrderSaga, TransportOrderFailed> SetTransportOrderFailedHandler() =>
            When(TransportOrderFailedEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId}/{CorrelationId} has failed with code {ErrorCode}", c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId, c.Message.Error)
                )
                .Publish(c => new HostOrderHandlingFailed(c.Saga.CorrelationId, c.Saga.OrderDetails.OrderId, HostOrderErrorCode.Any))
                .TransitionTo(OrderFailed)
                .Then(c => c.Saga.FailedReason = c.Message.Error.ToString());

        /// <summary>
        /// Handler for inbound message TransportOrderStarted
        /// </summary>
        /// <returns>Event Binding for TransportOrderStarted</returns>
        private EventActivityBinder<OrderSaga, TransportOrderStarted> SetTransportOrderStartedHandler() =>
            When(TransportOrderStartedEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId}/{CorrelationId} has been started", c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId))
                .Then(SendHostOrderStartedMessage);

        private void SendHostOrderStartedMessage(BehaviorContext<OrderSaga, TransportOrderStarted> context)
        {
            if (context.Saga.AssignedAgv != null)
            {
                var agv = _agvRegistry.GetAgv(context.Saga.AssignedAgv.Value);
                if (agv != null)
                {
                    context.Publish(new HostOrderStarted(context.Saga.CorrelationId, context.Saga.OrderDetails.OrderId, agv.AgvId));
                    context.TransitionToState(TransportOrderStarted);
                }
                else
                {
                    _logger.LogError("Missing AgvId for {CorrelationId}", context.Saga.CorrelationId);
                }
            }
            else
            {
                _logger.LogError("Corresponding AGV not found for Order {OrderId}/{CorrelationId}", context.Saga.OrderDetails.OrderId, context.Saga.CorrelationId);
            }
        }

        /// <summary>
        /// Handler for inbound message TransportOrderPickCompleted
        /// </summary>
        /// <returns>Event Binding for TransportOrderPickCompleted</returns>
        private EventActivityBinder<OrderSaga, TransportOrderPickCompleted> SetTransportOrderPickCompletedHandler() =>
            When(TransportOrderPickCompletedEvent)
                .Then(c => 
                    _logger.LogInformation("Pick of Order {OrderId}/{CorrelationId} has been completed", c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId))
            .TransitionTo(TransportOrderPicked);
    }
}
