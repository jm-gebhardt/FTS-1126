using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
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

        /// <summary>
        /// CancelTransportOrder has been received
        /// </summary>
        public Event<CancelHostOrder> CancelHostOrderEvent { get; private set; } = default!;

        /// <summary>
        /// TransportOrderReset has been received
        /// </summary>
        public Event<TransportOrderReset> TransportOrderResetEvent { get; private set; } = default!;
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

            Initially(SetHandleHostOrderHandler());
            During(OrderReceived, SetRequestAgvAssignHandler());
            During(AgvRequested, TransportOrderSend, TransportOrderStarted, SetRequestAgvAssignHandler_When_Not_OrderReceived());
            During(AgvRequested, SetAgvAssignSucceededHandler());
            During(AgvRequested, SetAgvAssignFailedHandler());
            During(TransportOrderSend, TransportOrderPicked, SetTransportOrderFailedHandler());
            During(TransportOrderSend, SetTransportOrderStartedHandler());
            During(TransportOrderStarted, SetTransportOrderPickCompletedHandler());
            During(TransportOrderPicked, SetTransportOrderCompletedHandler());
            During(OrderFailed, Final, SetTransportOrderResetHandler());

            Initially(SetCancelHostOrderHandler());
            DuringAny(SetCancelHostOrderHandler());
            During(Final, SetCancelHostOrderHandler());

            ConnectStateObserver(orderStateObserver);
            ConnectEventObserver(orderEventObserver);
        }

        #region Init
        /// <summary>
        /// Set saga factory on HandleHostOrderEvent
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

        private EventActivityBinder<OrderSaga, HandleHostOrder> SetHandleHostOrderHandler() =>
            When(HandleHostOrderEvent)
                .TransitionTo(OrderReceived)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} received", nameof(HandleHostOrder), c.Message.OrderDetails.OrderId, c.Saga.CorrelationId))
                .Publish(c => new RequestOrderAgvAssign(c.Saga.CorrelationId));

        private EventActivityBinder<OrderSaga, RequestOrderAgvAssign> SetRequestAgvAssignHandler() =>
            When(RequestOrderAgvAssignEvent)
                .Publish(c =>
                    new AssignAgv(c.Saga.CorrelationId, c.Saga.OrderDetails)
                )
                .TransitionTo(AgvRequested);

        private EventActivityBinder<OrderSaga, RequestOrderAgvAssign> SetRequestAgvAssignHandler_When_Not_OrderReceived() =>
            When(RequestOrderAgvAssignEvent)
                .Then(c => _logger.LogWarning("RequestOrderAgvAssign received but Order {OrderId} has already the state {State}",
                    c.Saga.OrderDetails.OrderId, c.Saga.CurrentState))
                .ThenAsync(ResendAgvBecameIdle);

        private async Task ResendAgvBecameIdle(BehaviorContext<OrderSaga, RequestOrderAgvAssign> context)
        {
            IQueryable<IAgv> idleAgvs = _agvRegistry.GetIdleAgvs();
            if (idleAgvs.Any())
            {
                var agv = idleAgvs.First();
                await context.Publish(new AgvBecameIdle(agv.CorrelationId));
                _logger.LogInformation("AgvRegistry: {countIdleAgvs} idle AGVS found. Resending -AgvBecameIdle- for AGV {AgvId} / {CorrelationId}",
                    idleAgvs.Count(), agv.AgvId, agv.CorrelationId);
            }
        }

        private EventActivityBinder<OrderSaga, AgvAssignmentSucceeded> SetAgvAssignSucceededHandler() =>
            When(AgvAssignSucceededEvent)
                .Then(c =>
                    _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} to AGV {AgvId}",
                        nameof(AgvAssignmentSucceeded), c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId, c.Message.AgvId)
                 )
                .Then((context) =>
                {
                    context.Saga.AssignedAgv = context.Message.AgvCorrelationId;
                })
                .Publish(c =>
                    new ExecuteTransportOrder(c.Message.AgvCorrelationId, c.Saga.OrderDetails, c.Message.AgvId, c.Message.Nodes)
                )
                .TransitionTo(TransportOrderSend);

        private EventActivityBinder<OrderSaga, AgvAssignmentFailed> SetAgvAssignFailedHandler() =>
            When(AgvAssignFailedEvent)
                .Then(c => c.Saga.FailedReason = $"{nameof(AgvAssignmentFailed)}.{c.Message.Error}")
                .ThenAsync(async c =>
                {
                    switch (c.Message.Error) // TransitionTo(OrderReceived or OrderFailed)
                    {
                        case AgvAssignmentFailed.ErrorCode.NoIdleAgv:
                            await c.TransitionToState(OrderReceived);
                            _logger.LogInformation("{MessageType}/{FailedReason}: Incoming Order {OrderId} / {CorrelationId} but no idle AGV found in AgvRegistry",
                                nameof(AgvAssignmentFailed), c.Message.Error, c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId);
                            break;

                        case AgvAssignmentFailed.ErrorCode.NoRouteFound:
                            await c.Publish(new HostOrderHandlingFailed(c.Saga.CorrelationId, c.Saga.OrderDetails.OrderId, HostOrderHandlingFailed.ErrorCode.InvalidOrderData));
                            await c.TransitionToState(OrderFailed);
                            _logger.LogInformation("{MessageType}/{FailedReason}: Incoming Order {OrderId} / {CorrelationId} but unable to calculate route with found AGVs in AgvRegistry",
                                nameof(AgvAssignmentFailed), c.Message.Error, c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId);
                            break;

                        case AgvAssignmentFailed.ErrorCode.AgvRegistryEmpty:
                            await c.TransitionToState(OrderReceived);
                            _logger.LogInformation("{MessageType}/{FailedReason}: Incoming Order {OrderId} / {CorrelationId} but no discovered AGV found in AgvRegistry",
                                nameof(AgvAssignmentFailed), c.Message.Error, c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId);
                            break;
                    }
                });

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
                .Then(c => // TransitionTo(TransportOrderStarted)
                {
                    if (c.Saga.AssignedAgv != null)
                    {
                        var agv = _agvRegistry.GetAgv(c.Saga.AssignedAgv.Value);
                        if (agv != null)
                        {
                            c.Saga.FailedReason = null;
                            c.Publish(new HostOrderStarted(c.Saga.CorrelationId, c.Saga.OrderDetails.OrderId, agv.AgvId));
                            c.TransitionToState(TransportOrderStarted);
                        }
                        else
                        {
                            _logger.LogError("Missing AgvId for {CorrelationId}", c.Saga.CorrelationId);
                        }
                    }
                    else
                    {
                        _logger.LogError("Corresponding AGV not found for Order {OrderId} / {CorrelationId}", c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId);
                    }
                });

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
                .ThenAsync(async (context) =>
                {
                    if (context.Saga.AssignedAgv != null)
                    {
                        await context.Publish(new ReleaseAgv(correlationId: context.Saga.AssignedAgv.Value));
                    }
                })
                .ThenAsync(async (context) =>
                {
                    await context.Publish(new HostOrderHandlingSucceeded(context.Saga.CorrelationId, context.Saga.OrderDetails.OrderId));
                })
                .Finalize(); // TransitionTo(Final)

        private EventActivityBinder<OrderSaga, TransportOrderReset> SetTransportOrderResetHandler() =>
            When(TransportOrderResetEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId} / {CorrelationId} was reset", c.Saga.OrderDetails.OrderId, c.Saga.CorrelationId)
                 )
                .Then(c => c.Saga.FailedReason = $"{nameof(TransportOrderReset)}")
                .TransitionTo(OrderReceived)
                .Publish(c => new RequestOrderAgvAssign(c.Saga.CorrelationId));

        private EventActivityBinder<OrderSaga, CancelHostOrder> SetCancelHostOrderHandler() =>
            When(CancelHostOrderEvent)
                .ThenAsync(async (context) =>
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
                        _logger.LogWarning("Unable to cancel Order {OrderId} / {CorrelationId} because it already is finished", context.Message.OrderId, context.Saga.CorrelationId);
                    }
                    else if (context.Saga.CurrentState == nameof(Initial))
                    {
                        await context.Publish(new HostOrderCancelFailed(context.Saga.CorrelationId, context.Message.OrderId, HostOrderCancelFailed.ErrorCode.OrderDoesNotExist));
                        _logger.LogWarning("Unable to cancel Order {OrderId} / {CorrelationId} because state is Initial => No Order in OrderRegistry",
                            context.Message.OrderId, context.Saga.CorrelationId);
                    }
                    else if (context.Saga.CurrentState == nameof(OrderFailed))
                    {
                        await context.Publish(new HostOrderCancelFailed(context.Saga.CorrelationId, context.Message.OrderId, HostOrderCancelFailed.ErrorCode.OrderAlreadyFailed));
                        _logger.LogError("Unable to cancel Order {OrderId} / {CorrelationId} because it already has failed", context.Message.OrderId, context.Saga.CorrelationId);
                    }
                    else if (context.Saga.CurrentState == nameof(Cancelled))
                    {
                        await context.Publish(new HostOrderCancelFailed(context.Saga.CorrelationId, context.Message.OrderId, HostOrderCancelFailed.ErrorCode.OrderAlreadyCancelled));
                        _logger.LogError("Unable to cancel Order {OrderId} / {CorrelationId} because it already has been cancelled", context.Message.OrderId, context.Saga.CorrelationId);
                    }
                    else
                    {
                        await context.Publish(new HostOrderCancelFailed(context.Saga.CorrelationId, context.Message.OrderId, HostOrderCancelFailed.ErrorCode.OrderAlreadyStarted));
                        _logger.LogError("Unable to cancel Order {OrderId} / {CorrelationId} because it already has started", context.Message.OrderId, context.Saga.CorrelationId);
                    }
                });
        #endregion
    }
}
