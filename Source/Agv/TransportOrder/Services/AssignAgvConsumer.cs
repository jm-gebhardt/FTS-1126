using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;

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
        /// On success, the AGV will be reserved and <see cref="AgvAssignmentSucceeded"/> is published.
        /// Otherwise, <see cref="AgvAssignmentFailed"/> is published.
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<AssignAgv> context)
        {
            _logger.LogInformation("Order {OrderId}/{CorrelationId} requests assignment to an AGV", context.Message.Order.OrderId, context.Message.CorrelationId);

            KeyValuePair<IAgv, IGraphAlgorithm.Route>? assignAgvAndRoute = await AssignIdleAgv<AStar>(context.Message.Order, context.CancellationToken);
            if (assignAgvAndRoute == null)
            {
                var error = CheckRouteExists<AStar>(context.Message.Order)? AgvAssignmentErrorCode.NoIdleAgv : AgvAssignmentErrorCode.NoRouteFound;
                await context.Publish(new AgvAssignmentFailed(context.Message.CorrelationId, error));
            }
            else
            {
                var agv = assignAgvAndRoute.Value.Key;
                var route = assignAgvAndRoute.Value.Value;
                
                await context.Publish( new AgvAssignmentSucceeded(
                    context.Message.CorrelationId,
                    agv.AgvId,
                    agv.CorrelationId,
                    route.Nodes
                ));       
            }
        }

        /// <summary>
        /// Try to assign the order to an AGV, the assigned AGV will be reserved
        /// </summary>
        /// <param name="order"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TGraphAlgorithm"></typeparam>
        /// <returns></returns>
        private async Task<KeyValuePair<IAgv, IGraphAlgorithm.Route>?> AssignIdleAgv<TGraphAlgorithm>(Order order, CancellationToken cancellationToken) where TGraphAlgorithm : IGraphAlgorithm
        {
            
            var agvChooser = new AgvChooser<TGraphAlgorithm>();
            IAgv? assignedAgv = null;
            IGraphAlgorithm.Route? route = null;
            
            List<IAgv> idleAgvs = _agvRegistry.GetIdleAgvs()
                .Where(agv => agv.AgvGraph != null)
                .ToList();
            
            while (idleAgvs.Count > 0)
            {
                KeyValuePair<Guid, IGraphAlgorithm.Route>? chosenAgv = agvChooser.ChooseAgv(idleAgvs, order);
                
                if (chosenAgv == null)
                {
                    _logger.LogInformation("Could not find any AGV to handle {OrderId}", order.OrderId);
                    break;
                }

                var correlationId = chosenAgv.Value.Key;

                if (await ReserveAgv(correlationId, cancellationToken))
                {
                    assignedAgv = idleAgvs.Single(x => x.CorrelationId == correlationId);
                    route = chosenAgv.Value.Value;
                    break;
                }
                _logger.LogInformation("Could not reserve Agv {CorrelationId}, trying next", correlationId);
                idleAgvs.RemoveAll(x => x.CorrelationId == correlationId);
            }
            
           
            if (assignedAgv == null || route == null)
            {
                // No route found
                return null;
            }
            
            return new KeyValuePair<IAgv, IGraphAlgorithm.Route>(assignedAgv, route);
        }

        /// <summary>
        /// Reserve AGV
        /// </summary>
        /// <param name="correlationId">Correlation ID of the AGV</param>
        /// <param name="cancellationToken"></param>
        /// <returns>True in case the agv reservation was successful</returns>
        private async Task<bool> ReserveAgv(Guid correlationId, CancellationToken cancellationToken)
        {
            Response<AgvReservationSucceeded, AgvReservationRejected> response = await _client.GetResponse<AgvReservationSucceeded, AgvReservationRejected>(
                new ReserveAgv(correlationId),
                cancellationToken);

            return response.Message is AgvReservationSucceeded;
        }

        /// <summary>
        /// Check if any of the registered AGVs has a route for the provided Order
        /// </summary>
        /// <param name="order"></param>
        /// <typeparam name="TGraphAlgorithm"></typeparam>
        /// <returns></returns>
        private bool CheckRouteExists<TGraphAlgorithm>(Order order) where TGraphAlgorithm : IGraphAlgorithm
        {
            var agvChooser = new AgvChooser<TGraphAlgorithm>();
            IEnumerable<IAgv> agvs = _agvRegistry.GetAllAgvs()
                .Where(agv => agv.AgvGraph != null);

            return agvs.Any(agv => agvChooser.CalculateRoute(agv, order) != null);
        }
    }
}
