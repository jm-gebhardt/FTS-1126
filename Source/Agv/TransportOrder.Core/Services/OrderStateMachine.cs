using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Messages;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// Order State Machine to orchestrate AGV transport orders 
    ///
    /// This is implemented as a MassTransit StateMachine <see href="https://masstransit-project.com/usage/sagas/automatonymous.html"/>
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public sealed class OrderStateMachine : MassTransitStateMachine<OrderSaga>
    {
        #region States
        /// <summary>
        /// State when Order has been received
        /// </summary>
        public State OrderReceived { get; private set; } = default!;

        /// <summary>
        /// Order has been split into actions and these remain in the queue until they have been assigned to an Agv and completed
        /// </summary>
        public State OrderQueued { get; private set; } = default!;

        /// <summary>
        /// State when the pick action has been assigned to an Agv
        /// </summary>
        public State TransportOrderSent { get; private set; } = default!;

        /// <summary>
        /// State when pick action has been started by an Agv
        /// </summary>
        public State TransportOrderStarted { get; private set; } = default!;

        /// <summary>
        /// State when pick action has been completed, i.e. the item has been picked
        /// </summary>
        public State TransportOrderPicked { get; private set; } = default!;

        /// <summary>
        /// State when the OrderFailed has been received
        /// </summary>
        public State OrderFailed { get; private set; } = default!;

        /// <summary>
        /// State when TransportOrder has been cancelled
        /// </summary>
        public State Cancelled { get; private set; } = default!;
        #endregion

        #region Events
        /// <summary>
        /// HandleHostOrder was received. This is the Initial Event of the OrderStateMachine
        /// </summary>
        public Event<HandleHostOrder> HandleHostOrderEvent { get; private set; } = default!;

        /// <summary>
        /// Triggered when the order has been split into actions
        /// </summary>
        public Event<OrderQueuedEvent> OrderQueuedEvent { get; private set; } = default!;

        /// <summary>
        /// OrderAssignmentSucceeded was received
        /// </summary>
        public Event<OrderAssignmentSucceeded> OrderAssignmentSucceededEvent { get; private set; } = default!;

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

        /// <summary>
        /// CancelTransportOrder has been received
        /// </summary>
        public Event<CancelHostOrder> CancelHostOrderEvent { get; private set; } = default!;

        /// <summary>
        /// ResetTransportOrder has been received
        /// </summary>
        public Event<ResetTransportOrder> ResetTransportOrderEvent { get; private set; } = default!;

        /// <summary>
        /// ManuallyCompleteTransportOrder has been received
        /// </summary>
        public Event<ManuallyCompleteTransportOrder> ManuallyCompleteTransportOrderEvent { get; private set; } = default!;
        #endregion

        private readonly ILogger<OrderStateMachine> _logger;
        private readonly IAgvRegistry _agvRegistry;

        /// <summary>
        /// OrderStateMachine
        /// </summary>
        public OrderStateMachine(ILogger<OrderStateMachine> logger, IAgvRegistry agvRegistry, IStateObserver<OrderSaga> orderStateObserver, IEventObserver<OrderSaga> orderEventObserver)
        {
            _logger = logger;
            _agvRegistry = agvRegistry;

            SetSagaFactory();
            InstanceState(x => x.CurrentState);

            Initially(SetHandleHostOrderHandler());
            During(OrderReceived, SetOrderQueuedEventHandler());
            During(OrderQueued, SetOrderAssignmentSucceededHandler());
            During(TransportOrderSent, SetTransportOrderStartedHandler());
            During(TransportOrderStarted, SetTransportOrderPickCompletedHandler());
            During(TransportOrderPicked, TransportOrderStarted, SetTransportOrderCompletedHandler());

            During(TransportOrderSent, TransportOrderPicked, SetTransportOrderFailedHandler());

            DuringAny(SetCancelHostOrderHandler());
            During(Final, SetCancelHostOrderHandler());
            Initially(SetCancelHostOrderHandler());

            During(OrderReceived, OrderFailed, Final, SetResetTransportOrderHandler());

            DuringAny(SetManuallyCompleteTransportOrderHandler());
            During(Final, SetManuallyCompleteTransportOrderHandler());

            ConnectStateObserver(orderStateObserver);
            ConnectEventObserver(orderEventObserver);
        }

        #region Init
        /// <summary>
        /// Set saga factory on <see cref="HandleHostOrder"/> or <see cref="CancelHostOrder"/>
        /// </summary>
        private void SetSagaFactory()
        {
            // Initial creation
            Event(() => HandleHostOrderEvent, e =>
                e.SetSagaFactory(c => new OrderSaga(c.Message.CorrelationId, c.Message.OrderDetails))
            );

            Event(() => CancelHostOrderEvent, e =>
                e.SetSagaFactory(context => new OrderSaga(context.Message.CorrelationId))
            );
        }
        #endregion

        #region State transitions
        /// <summary>
        /// Handler for <see cref="OrderQueuedEvent"/>
        /// </summary>
        /// <returns>Event Binding for <see cref="OrderQueuedEvent"/></returns>
        private EventActivityBinder<OrderSaga, OrderQueuedEvent> SetOrderQueuedEventHandler() =>
            When(OrderQueuedEvent)
                .Then(_ =>
                {
                    _logger.LogInformation("{nameOf}", nameof(SetOrderQueuedEventHandler));
                })
                .TransitionTo(OrderQueued);

        /// <summary>
        /// Handler for HandleHostOrder
        /// </summary>
        /// <returns>Event Binding for HandleHostOrder</returns>
        private EventActivityBinder<OrderSaga, HandleHostOrder> SetHandleHostOrderHandler() =>
            When(HandleHostOrderEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(HandleHostOrder), c.Message.OrderDetails.OrderId, c.Saga.CorrelationId))
                .Then(UpdateSaga)
                .TransitionTo(OrderReceived)
                .Publish(OrderQueuingCommand);

        private EventActivityBinder<OrderSaga, OrderAssignmentSucceeded> SetOrderAssignmentSucceededHandler() =>
            When(OrderAssignmentSucceededEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} to AGV {AssignedAgvId}",
                        nameof(OrderAssignmentSucceeded), c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId, c.Message.AgvId)
                )
                .Then(UpdateSagaState)
                .TransitionTo(TransportOrderSent);

        private EventActivityBinder<OrderSaga, TransportOrderFailed> SetTransportOrderFailedHandler() =>
            When(TransportOrderFailedEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId}", nameof(TransportOrderFailed), c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId)
                 )
                .Publish(c => new HostOrderHandlingFailed(c.Saga.CorrelationId, c.Saga.OrderDetails.OrderId, HostOrderHandlingFailed.ErrorCode.FailedByTransportLayer))
                .TransitionTo(OrderFailed)
                .Then(c => c.Saga.FailedReason = nameof(TransportOrderFailed));

        private EventActivityBinder<OrderSaga, TransportOrderStarted> SetTransportOrderStartedHandler() =>
            When(TransportOrderStartedEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId}", nameof(TransportOrderStarted), c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId))
                .Publish(c => new HostOrderStarted(c.Saga.CorrelationId, c.Saga.OrderDetails.OrderId, c.Saga.AssignedAgvId!))
                .TransitionTo(TransportOrderStarted);

        private EventActivityBinder<OrderSaga, TransportOrderPickCompleted> SetTransportOrderPickCompletedHandler() =>
            When(TransportOrderPickCompletedEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} has been completed", nameof(TransportOrderPickCompleted), c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId))
                .TransitionTo(TransportOrderPicked);

        private EventActivityBinder<OrderSaga, TransportOrderCompleted> SetTransportOrderCompletedHandler() =>
            When(TransportOrderCompletedEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId} / {CorrelationId} has been completed", c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId)
                )
                .ThenAsync(SendHostOrderCompleted)
                .Finalize();

        private EventActivityBinder<OrderSaga, ResetTransportOrder> SetResetTransportOrderHandler() =>
            When(ResetTransportOrderEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId} / {CorrelationId} was reset", c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId)
                 )
                .Then(c => c.Saga.FailedReason = $"{nameof(ResetTransportOrder)}")
                .Then(c =>
                {
                    // reset any assignment
                    c.Saga.AssignedAgvId = null;
                    c.Saga.AssignedAgv = null;
                })
                .TransitionTo(OrderReceived)
                .Publish(OrderQueuingCommand);

        private EventActivityBinder<OrderSaga, ManuallyCompleteTransportOrder> SetManuallyCompleteTransportOrderHandler() =>
            When(ManuallyCompleteTransportOrderEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId} / {CorrelationId} was completed manually", c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId)
                )
                .ThenAsync(SendHostOrderManuallyCompleted)
                .Finalize();

        /// <summary>
        /// Handler for inbound message CancelTransportOrder
        /// </summary>
        /// <returns>Event Binding for CancelTransportOrder</returns>
        private EventActivityBinder<OrderSaga, CancelHostOrder> SetCancelHostOrderHandler() =>
            When(CancelHostOrderEvent)
                .ThenAsync(CancelHostOrderExamination);
        #endregion

        #region Actions
        private void UpdateSagaState(BehaviorContext<OrderSaga, OrderAssignmentSucceeded> context)
        {
            context.Saga.AssignedAgv = context.Message.AgvCorrelationId;
            context.Saga.AssignedAgvId = context.Message.AgvId;
        }

        private void UpdateSaga(BehaviorContext<OrderSaga, HandleHostOrder> context)
        {
            context.Saga.OrderDetails = context.Message.OrderDetails;
        }

        private async Task SendHostOrderCompleted(BehaviorContext<OrderSaga, TransportOrderCompleted> context)
        {
            await context.Publish(new HostOrderHandlingSucceeded(context.Saga.CorrelationId, context.Saga.OrderDetails.OrderId));
        }

        private async Task SendHostOrderManuallyCompleted(BehaviorContext<OrderSaga, ManuallyCompleteTransportOrder> context)
        {
            await context.Publish(new HostOrderHandlingSucceeded(context.Saga.CorrelationId, context.Saga.OrderDetails.OrderId));
        }

        private static OrderQueuingCommand OrderQueuingCommand<T>(BehaviorContext<OrderSaga, T> c) where T : class
        {
            return new OrderQueuingCommand(
                c.Saga.CorrelationId,
                c.Saga.OrderDetails
            );
        }

        private async Task CancelHostOrderExamination(BehaviorContext<OrderSaga, CancelHostOrder> context)
        {
            if (context.Saga.CurrentState == nameof(OrderReceived))
            {
                await context.Publish(new HostOrderCancelSucceeded(context.Saga.CorrelationId, context.Message.OrderId));
                await context.TransitionToState(Cancelled);
                _logger.LogInformation("Order {OrderId} / {CorrelationId} is successfully cancelled in OrderRegistry",
                    context.Saga.OrderDetails.OrderId, context.Saga.CorrelationId);
            }
            else if (context.Saga.CurrentState == nameof(Final))
            {
                await context.Publish(new HostOrderCancelFailed(context.Saga.CorrelationId, context.Message.OrderId, HostOrderCancelFailed.ErrorCode.OrderAlreadyFinished));
                _logger.LogWarning("Unable to cancel order {OrderId} / {CorrelationId} because it already is finished", context.Message.OrderId, context.Saga.CorrelationId);
            }
            else if (context.Saga.CurrentState == nameof(Initial))
            {
                await context.Publish(new HostOrderCancelFailed(context.Saga.CorrelationId, context.Message.OrderId, HostOrderCancelFailed.ErrorCode.OrderDoesNotExist));
                _logger.LogWarning("Unable to cancel order {OrderId} / {CorrelationId} because order is in inital state => No Order in OrderRegistry",
                    context.Message.OrderId, context.Saga.CorrelationId);
            }
            else if (context.Saga.CurrentState == nameof(OrderFailed))
            {
                await context.Publish(new HostOrderCancelFailed(context.Saga.CorrelationId, context.Message.OrderId, HostOrderCancelFailed.ErrorCode.OrderAlreadyFailed));
                _logger.LogError("Unable to cancel order {OrderId} / {CorrelationId} because it already has been failed", context.Message.OrderId, context.Saga.CorrelationId);
            }
            else if (context.Saga.CurrentState == nameof(Cancelled))
            {
                await context.Publish(new HostOrderCancelFailed(context.Saga.CorrelationId, context.Message.OrderId, HostOrderCancelFailed.ErrorCode.OrderAlreadyCancelled));
                _logger.LogError("Unable to cancel order {OrderId} / {CorrelationId} because it already has been cancelled", context.Message.OrderId, context.Saga.CorrelationId);
            }
            else
            {
                await context.Publish(new HostOrderCancelFailed(context.Saga.CorrelationId, context.Message.OrderId, HostOrderCancelFailed.ErrorCode.OrderAlreadyStarted));
                _logger.LogError("Unable to cancel order {OrderId} / {CorrelationId} because it already has been started", context.Message.OrderId, context.Saga.CorrelationId);
            }
        }
        #endregion

    }
}
