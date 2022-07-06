using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
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
        private readonly IAgvRegistry _agvRegistry;
        private readonly IRequestClient<ReserveAgv> _client;

        /// <summary>
        /// Consumer for <see cref="IAssignAgv"/> messages
        /// </summary>
        public AssignAgvConsumer(ILogger<AssignAgvConsumer> logger, 
            IAgvRegistry agvRegistry,
            IRequestClient<ReserveAgv> client)
        {
            _logger = logger;
            _agvRegistry = agvRegistry;
            _client = client;
        }

        /// <summary>
        /// Respond to <see cref="IAssignAgv"/> message
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<IAssignAgv> context)
        {
            _logger.LogInformation("Order {OrderId}/{CorrelationId} requests assignment to an AGV", context.Message.Order.OrderId, context.Message.CorrelationId);
            
            IAgv? assignedAgv = null;
            List<IAgv> idleAgvs = _agvRegistry.GetIdleAgvs().ToList();
            
            while (assignedAgv == null)
            {
                // Simply take the first AGV, algorithm needed
                var agv = idleAgvs.FirstOrDefault();
                if (agv == null)
                {
                    // No agv left to try
                    break;
                }
                idleAgvs.Remove(agv);
                
                Response<AgvReservationSucceeded, AgvReservationRejected> response = await _client.GetResponse<AgvReservationSucceeded, AgvReservationRejected>(
                    new ReserveAgv(agv.CorrelationId), 
                    context.CancellationToken);
                
                if (response.Message is AgvReservationSucceeded)
                {
                    assignedAgv = agv;
                }
                else
                {
                    _logger.LogInformation("Could not reserve Agv {CorrelationId}, trying next", agv.CorrelationId);
                }
            }

            if (assignedAgv != null)
            {
                await context.Publish<IAgvAssignmentSucceeded>(new
                {
                    context.Message.CorrelationId,
                    assignedAgv.AgvId,
                    AgvCorrelationId = assignedAgv.CorrelationId
                });    
            }
            else
            {
                await context.Publish(new AgvAssignmentFailed(context.Message.CorrelationId));    
            }
        }
    }
}