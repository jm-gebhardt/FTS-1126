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

            var agvChooser = new AgvChooser<AStar>();
            IAgv? assignedAgv = null;
            List<IAgv> idleAgvs = _agvRegistry.GetIdleAgvs().ToList();
            
            while (idleAgvs.Count > 0)
            {
                Guid? agvGuid = agvChooser.ChooseAgv(idleAgvs, context.Message.Order);

                if (agvGuid == null)
                {
                    _logger.LogInformation("Could not find any AGV to handle {OrderId}", context.Message.Order.OrderId);
                    break;
                }

                Response<AgvReservationSucceeded, AgvReservationRejected> response = await _client.GetResponse<AgvReservationSucceeded, AgvReservationRejected>(
                    new ReserveAgv(agvGuid.Value),
                    context.CancellationToken);
                if (response.Message is AgvReservationSucceeded)
                {
                    assignedAgv = idleAgvs.Single(x => x.CorrelationId == agvGuid);
                    break;
                }
                else
                {
                    _logger.LogInformation("Could not reserve Agv {CorrelationId}, trying next", agvGuid);
                    idleAgvs.RemoveAll(x => x.CorrelationId == agvGuid);
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