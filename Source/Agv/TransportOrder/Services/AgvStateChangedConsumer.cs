using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Consumer for <see cref="IAgvStateChanged"/> messages
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AgvStateChangedConsumer : IConsumer<IAgvStateChanged>
    {
        private readonly ILogger<AgvStateChangedConsumer> _logger;
        private readonly InMemoryAgvRegistry _agvRegistry;
        private readonly ConcurrentQueue<KeyValuePair<Guid, Order>> _orderQueue;

        /// <summary>
        /// Consumer for <see cref="IAgvStateChanged"/> messages
        /// </summary>
        public AgvStateChangedConsumer(ILogger<AgvStateChangedConsumer> logger, IAgvRegistry agvRegistry, ConcurrentQueue<KeyValuePair<Guid, Order>> orderQueue)
        {
            _logger = logger;
            _agvRegistry = (InMemoryAgvRegistry)agvRegistry;
            _orderQueue = orderQueue;
        }

        /// <summary>
        /// Respond to <see cref="IAgvStateChanged"/> message
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<IAgvStateChanged> context)
        {
            _logger.LogInformation("State for AGV {AgvId} changed from {PreviousState} to {CurrentState}",
                context.Message.AgvId, context.Message.PreviousState, context.Message.CurrentState);

            string? orderId = null;
            var agv = new Common.Messages.AgvManagement.Agv(context.Message.AgvId, context.Message.CurrentState);

            if (agv.State == AgvState.Idle)
            {
                lock (_agvRegistry)
                {
                    // Check if here are any waiting orders that can be assigned to this idle AGV
                    if (_orderQueue.TryDequeue(out KeyValuePair<Guid, Order> result))
                    {
                        orderId = result.Value.OrderId;
                        agv.State = AgvState.Reserved;
                    }
                }
            }

            _agvRegistry.AddOrUpdate(context.Message.CorrelationId, agv);

            if (orderId != null) // Found waiting order for idle agv -> send IAgvAssignSucceeded message
            {
                var correlationId = orderId.ToGuid();

                await context.Publish<IAgvAssignSucceeded>(new
                {
                    CorrelationId = correlationId,
                    OrderId = orderId,
                    AssignedAgv = context.Message.AgvId
                });
            }
        }
    }
}