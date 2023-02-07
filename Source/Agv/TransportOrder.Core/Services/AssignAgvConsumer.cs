using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using MassTransit;
using Microsoft.Extensions.Logging;
using IRoutingPlausibilityChecker = Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing.IRoutingPlausibilityChecker;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
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
        private readonly IRoutingPlausibilityChecker _routingPlausibilityChecker;

        /// <summary>
        /// Consumer for <see cref="AssignAgv"/> messages
        /// </summary>
        public AssignAgvConsumer(ILogger<AssignAgvConsumer> logger,
            IAgvRegistry agvRegistry,
            IRequestClient<ReserveAgv> client,
            IRoutingPlausibilityChecker routingPlausibilityChecker)
        {
            _logger = logger;
            _agvRegistry = agvRegistry;
            _client = client;
            _routingPlausibilityChecker = routingPlausibilityChecker;
        }

        /// <summary>
        /// Respond to <see cref="AssignAgv"/> message
        /// On success, the AGV will be reserved and <see cref="AgvAssignmentSucceeded"/> is published.
        /// Otherwise, <see cref="AgvAssignmentFailed"/> is published.
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<AssignAgv> context)
        {
            _logger.LogDebug("Request {MessageType} received for order {OrderId} / {CorrelationId}",
                nameof(AssignAgv), context.Message.Order.OrderId, context.Message.Order.CorrelationId);

            KeyValuePair<IAgv, IGraphAlgorithm.Route>? assignedAgvAndRoute = await AssignIdleAgv<AStar>(context.Message.Order, context.CancellationToken);
            if (assignedAgvAndRoute == null)
            {
                var error = CheckRouteExists<AStar>(context.Message.Order) ? AgvAssignmentFailed.ErrorCode.NoIdleAgv : AgvAssignmentFailed.ErrorCode.NoRouteFound;

                if (_agvRegistry.Count == 0 && error == AgvAssignmentFailed.ErrorCode.NoRouteFound)
                {
                    error = AgvAssignmentFailed.ErrorCode.AgvRegistryEmpty;
                }

                _logger.LogDebug("{MessageType} for order {OrderId} / {CorrelationId}",
                    nameof(AssignAgv), context.Message.Order.OrderId, context.Message.Order.CorrelationId);
                await context.RespondAsync(new AgvAssignmentFailed(context.Message.CorrelationId, error));
            }
            else
            {
                var agv = assignedAgvAndRoute.Value.Key;
                var route = assignedAgvAndRoute.Value.Value;

                _logger.LogInformation("{MessageType} for order {OrderId} / {CorrelationId} using AGV {AgvId}",
                    nameof(AssignAgv), context.Message.Order.OrderId, context.Message.Order.CorrelationId, agv.AgvId);
                await context.RespondAsync(new AgvAssignmentSucceeded(
                    context.Message.CorrelationId,
                    agv.AgvId,
                    agv.CorrelationId,
                    route.Nodes
                ));
            }
        }

        /// <summary>
        /// Try to assign the order to an AGV, the assigned AGV will be reserved
        /// </summary><remarks>
        /// Takes the complete order into account because the AGV needs to be able to travel
        /// the complete route independent of the number of actions this order consists of.
        /// </remarks>
        /// <param name="order"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TGraphAlgorithm"></typeparam>
        /// <returns>Returns AGV and available route when reservable AGV found otherwise returns null</returns>
        private async Task<KeyValuePair<IAgv, IGraphAlgorithm.Route>?> AssignIdleAgv<TGraphAlgorithm>(OrderDetails order, CancellationToken cancellationToken) where TGraphAlgorithm : IGraphAlgorithm
        {
            var agvChooser = new AgvChooser<TGraphAlgorithm>(_routingPlausibilityChecker);
            IAgv? assignedAgv = null;
            IGraphAlgorithm.Route? route = null;

            List<IAgv> idleAgvs = _agvRegistry.GetIdleAgvs()
                .AsEnumerable()
                .Where(agv => agv.Assignable)
                .ToList();

            while (idleAgvs.Count > 0)
            {
                KeyValuePair<Guid, IGraphAlgorithm.Route>? chosenAgv = agvChooser.ChooseAgv(idleAgvs, order);

                if (chosenAgv == null)
                {
                    _logger.LogInformation("Could not find any AGV to handle {OrderId} / {CorrelationId}",
                        order.OrderId, order.CorrelationId);
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
                // No idle AGV present or no route found
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
        private bool CheckRouteExists<TGraphAlgorithm>(OrderDetails order) where TGraphAlgorithm : IGraphAlgorithm
        {
            var agvChooser = new AgvChooser<TGraphAlgorithm>(_routingPlausibilityChecker);
            IEnumerable<IAgv> agvs = _agvRegistry.GetAllAgvs()
                .Where(agv => agv.AgvGraph != null);

            return agvs.Any(agv => agvChooser.CalculateRoute(agv, order) != null);
        }

    }
}
