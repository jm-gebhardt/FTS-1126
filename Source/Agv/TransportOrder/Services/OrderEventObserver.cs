using Gebhardt.StoreWare.Agv.TransportOrder.Db.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Observer class dedicated to track state transitions in OrderSaga
    /// </summary>
    public class OrderEventObserver : IEventObserver<OrderSaga>
    {
        private readonly ILogger<OrderEventObserver> _logger;

        private readonly IOrderAuditRegistry _orderAuditRegistry;

        private const string InitialState = "Initial";

        /// <summary>
        /// Full constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="orderAuditRegistry"></param>
        public OrderEventObserver(ILogger<OrderEventObserver> logger, IOrderAuditRegistry orderAuditRegistry)
        {
            _logger = logger;
            _orderAuditRegistry = orderAuditRegistry;
        }

        ///<inheritdoc/>
        public async Task PreExecute(BehaviorContext<OrderSaga> context)
        {
            await Task.FromResult(true);
        }

        ///<inheritdoc/>
        public async Task PreExecute<T>(BehaviorContext<OrderSaga, T> context) where T : class
        {
            if (context.Saga.CurrentState == InitialState && 
                await _orderAuditRegistry.Add(context.Saga, context.Event.Name, context.Saga.CurrentState, Hook.PreHook, context.CancellationToken))
            {
                _logger.LogDebug("PreExecute<{T}>: State -> {CurrentState} {eventName} {OrderSagaCorrelationId}",
                     typeof(T), context.Saga.CurrentState, context.Event.Name, context.Saga.CorrelationId);
            }
            await Task.FromResult(true);
        }
        
        ///<inheritdoc/>
        public async Task PostExecute(BehaviorContext<OrderSaga> context)
        {
            await Task.FromResult(true);
        }

        ///<inheritdoc/>
        public async Task PostExecute<T>(BehaviorContext<OrderSaga, T> context) where T : class
        {
            if (await _orderAuditRegistry.Add(context.Saga, context.Event.Name, context.Saga.CurrentState, Hook.PostHook, context.CancellationToken))
            {
                _logger.LogDebug("PostExecute<{T}>: State -> {CurrentState} {eventName} {OrderSagaCorrelationId}",
                    typeof(T), context.Saga.CurrentState, context.Event.Name, context.Saga.CorrelationId);
            }
            await Task.FromResult(true);
        }

        ///<inheritdoc/>
        public async Task ExecuteFault(BehaviorContext<OrderSaga> context, Exception exception)
        {
            await Task.FromResult(true);
        }

        ///<inheritdoc/>
        public async Task ExecuteFault<T>(BehaviorContext<OrderSaga, T> context, Exception exception) where T : class
        {
            if (await _orderAuditRegistry.Add(context.Saga, context.Event.Name, context.Saga.CurrentState, Hook.ExecuteFault, context.CancellationToken))
            {
                _logger.LogDebug("ExecuteFault<{T}>: State -> {CurrentState} {eventName} {OrderSagaCorrelationId}",
                    typeof(T), context.Saga.CurrentState, context.Event.Name, context.Saga.CorrelationId);
                _logger.LogError("{msg} \n {stackTrace}", exception.Message, exception.StackTrace);
            }
            await Task.FromResult(true);
        }
    }
}
