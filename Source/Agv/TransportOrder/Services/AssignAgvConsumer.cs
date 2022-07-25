using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Definitions;
namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Consumer for <see cref="AssignAgv"/> messages
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AssignAgvConsumer : IConsumer<AssignAgv>
    {
        private readonly ILogger<AssignAgvConsumer> _logger;
        private readonly IAgvRegistry _agvRegistry;
        private readonly IRequestClient<ReserveAgv> _client;

        /// <summary>
        /// Consumer for <see cref="AssignAgv"/> messages
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
        /// Respond to <see cref="AssignAgv"/> message
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<AssignAgv> context)
        {
            _logger.LogInformation("Order {OrderId}/{CorrelationId} requests assignment to an AGV", context.Message.Order.OrderId, context.Message.CorrelationId);

            var agvChooser = new AgvChooser<AStar>();
            IAgv? assignedAgv = null;
            IGraphAlgorithm.Route? route = null;
            
            List<IAgv> idleAgvs = _agvRegistry.GetIdleAgvs()
                .Where(agv => agv.AgvGraph != null)
                .ToList();
            
            while (idleAgvs.Count > 0)
            {
                KeyValuePair<Guid, IGraphAlgorithm.Route>? chosenAgv = agvChooser.ChooseAgv(idleAgvs, context.Message.Order);
                
                if (chosenAgv == null)
                {
                    _logger.LogInformation("Could not find any AGV to handle {OrderId}", context.Message.Order.OrderId);
                    break;
                }

                var correlationId = chosenAgv.Value.Key;
                
                Response<AgvReservationSucceeded, AgvReservationRejected> response = await _client.GetResponse<AgvReservationSucceeded, AgvReservationRejected>(
                    new ReserveAgv(correlationId),
                    context.CancellationToken);
                if (response.Message is AgvReservationSucceeded)
                {
                    assignedAgv = idleAgvs.Single(x => x.CorrelationId == correlationId);
                    route = chosenAgv.Value.Value;
                    break;
                }
                else
                {
                    _logger.LogInformation("Could not reserve Agv {CorrelationId}, trying next", correlationId);
                    idleAgvs.RemoveAll(x => x.CorrelationId == correlationId);
                }
            }

            if (assignedAgv != null && route != null)
            {
                await context.Publish( new AgvAssignmentSucceeded(
                    context.Message.CorrelationId,
                    assignedAgv.AgvId,
                    assignedAgv.CorrelationId,
                    route.Nodes
                ));    
            }
            else
            {
                await context.Publish(new AgvAssignmentFailed(context.Message.CorrelationId));
            }
        }
    }
}
