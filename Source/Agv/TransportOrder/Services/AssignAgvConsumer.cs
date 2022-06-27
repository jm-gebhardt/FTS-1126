using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{

    /// <summary>
    /// Consumer for <see cref="IAssignAgv"/> messages
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AssignAgvConsumer : IConsumer<IAssignAgv>
    {
        private readonly ILogger<AssignAgvConsumer> _logger;
        private readonly InMemoryAgvRegistry _agvRegistry;
        private readonly ConcurrentQueue<KeyValuePair<Guid, Order>> _orderQueue;

        /// <summary>
        /// Consumer for <see cref="IAssignAgv"/> messages
        /// </summary>
        public AssignAgvConsumer(ILogger<AssignAgvConsumer> logger, IAgvRegistry agvRegistry, ConcurrentQueue<KeyValuePair<Guid, Order>> orderQueue)
        {
            _logger = logger;
            _agvRegistry = (InMemoryAgvRegistry)agvRegistry;
            _orderQueue = orderQueue;
        }

        /// <summary>
        /// Respond to <see cref="IAssignAgv"/> message
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<IAssignAgv> context)
        {
            _logger.LogInformation("Order {OrderId}/{CorrelationId} requests assignment to an AGV", context.Message.Order.OrderId, context.Message.CorrelationId);

            var agv = _agvRegistry.ReserveIdleAgv();

            if (agv == null) // No agv available -> store order in registry
            {
                _logger.LogInformation("No free AGV available, storing {OrderId}/{CorrelationId} in queue", context.Message.Order.OrderId, context.Message.CorrelationId);

                _orderQueue.Enqueue(KeyValuePair.Create(context.Message.CorrelationId, context.Message.Order));
            }
            else
            {
                await context.Publish<IAgvAssignSucceeded>(new
                {
                    context.Message.CorrelationId,
                    context.Message.Order.OrderId,
                    AssignedAgv = agv.AgvId
                });
            }
        }
    }
}