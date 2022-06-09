using Gebhardt.StoreWare.Agv.Common.Messages.Agv.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Agv.Events;
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
        /// State when IHandleHostOrder has been received and IAssignAgv has been published
        /// </summary>
        public State AgvRequested { get; private set; } = default!;

        /// <summary>
        /// State when the IAgvAssignSucceeded has been received and IExecuteTransportOrder has been published
        /// </summary>
        public State TransportOrderSend { get; private set; } = default!;

        /// <summary>
        /// IHandleHostOrder was received. This is the Initial Event of the OrderStateMachine
        /// </summary>
        public Event<IHandleHostOrder> HandleHostOrderEvent { get; private set; } = default!;

        /// <summary>
        /// IAgvAssignSucceeded was received
        /// </summary>
        public Event<IAgvAssignSucceeded> AgvAssignSucceededEvent { get; private set; } = default!;

        /// <summary>
        /// ITransportOrderCompleted was received. This is a final event
        /// </summary>
        public Event<ITransportOrderCompleted> TransportOrderCompletedEvent { get; private set; } = default!;
        
        private readonly ILogger<OrderStateMachine> _logger;


        /// <summary>
        /// OrderStateMachine
        /// </summary>
        public OrderStateMachine(ILogger<OrderStateMachine> logger)
        {
            _logger = logger;

            SetupInitialCorrelation();
            Initially(SetHandleHostOrderHandler());
            During(AgvRequested, SetAgvAssignSucceededHandler());
            During(TransportOrderSend, SetTransportOrderCompletedHandler());
        }

        /// <summary>
        /// IHandleHostOrder may not provide a CorrelationId, if CorrelationId is empty, a new one is assigned
        /// Correlation is then performed using OrderId or a new CorrelationId is assigned
        /// <see href="https://masstransit-project.com/usage/sagas/automatonymous.html#event-2" />
        /// </summary>
        private void SetupInitialCorrelation()
        {
            // Initial creation
            Event(() => HandleHostOrderEvent, e =>
                e.SetSagaFactory(context => new OrderSaga(context.Message.Order, context.Message.CorrelationId))
            );
        }

        /// <summary>
        /// Handler for IHandleHostOrder
        /// </summary>
        /// <returns>Event Binding for IHandleHostOrder</returns>
        private EventActivityBinder<OrderSaga, IHandleHostOrder> SetHandleHostOrderHandler() =>
            When(HandleHostOrderEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId}/{CorrelationId} received", c.Message.Order.OrderId, c.Saga.CorrelationId))
                .PublishAsync(c =>
                    c.Init<IAssignAgv>(new {c.Saga.CorrelationId, c.Saga.Order})
                )
                .TransitionTo(AgvRequested);

        /// <summary>
        /// Handler for IAgvAssignSucceeded
        /// </summary>
        /// <returns>Event Binding for IAgvAssignSucceeded</returns>
        private EventActivityBinder<OrderSaga, IAgvAssignSucceeded> SetAgvAssignSucceededHandler() =>
            When(AgvAssignSucceededEvent)
                .Then(c => 
                    _logger.LogInformation("Order {OrderId}/{CorrelationId} has been assigned to {AssignedAgv}", 
                        c.Message.OrderId, c.Saga.CorrelationId, c.Message.AssignedAgv)
                 )
                .Then(c => UpdateSagaSate(c.Saga, c.Message))
                .PublishAsync(c =>
                    c.Init<IExecuteTransportOrder>(new { c.Saga.CorrelationId, c.Saga.Order, c.Message.AssignedAgv})
                )
                .PublishAsync( c =>
                    c.Init<IHostOrderStarted>(new {c.Saga.CorrelationId, c.Saga.Order.OrderId,  c.Message.AssignedAgv})
                )
                .TransitionTo(TransportOrderSend);


        /// <summary>
        /// Handler for IAgvAssignSucceeded
        /// </summary>
        /// <returns>Event Binding for IAgvAssignSucceeded</returns>
        private EventActivityBinder<OrderSaga, ITransportOrderCompleted> SetTransportOrderCompletedHandler() =>
            When(TransportOrderCompletedEvent)
                .Then(c =>
                    _logger.LogInformation("Order {OrderId}/{CorrelationId} has been completed",c.Saga.Order.OrderId, c.Saga.CorrelationId )
                 )
                .PublishAsync(c => c.Init<IHostOrderHandlingSucceeded>(new
                {
                    c.Saga.CorrelationId, c.Saga.Order.OrderId
                }))
                .Finalize();

        private void UpdateSagaSate(OrderSaga saga, IAgvAssignSucceeded agvAssignSucceeded)
        {
            saga.AssignedAgv = agvAssignSucceeded.AssignedAgv;
        }
    }
}
