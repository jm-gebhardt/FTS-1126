using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Consumer for <see cref="AssignAgv"/> messages
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AssignAgvConsumer : IConsumer<AssignAgv>, IConsumer<AgvBecameIdle>
    {
        private readonly ILogger<AssignAgvConsumer> _logger;
        private readonly IAgvRegistry _agvRegistry;
        private readonly IRequestClient<ReserveAgv> _client;
        private readonly IOrderRegistry _orderRegistry;

        /// <summary>
        /// Consumer for <see cref="AssignAgv"/> messages
        /// </summary>
        public AssignAgvConsumer(ILogger<AssignAgvConsumer> logger,
            IAgvRegistry agvRegistry,
            IOrderRegistry orderRegistry,
            IRequestClient<ReserveAgv> client)
        {
            _logger = logger;
            _agvRegistry = agvRegistry;
            _client = client;
            _orderRegistry = orderRegistry;
        }

        /// <summary>
        /// Respond to <see cref="AssignAgv"/> message
        /// On success, the AGV will be reserved and <see cref="AgvAssignmentSucceeded"/> is published.
        /// Otherwise, <see cref="AgvAssignmentFailed"/> is published.
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<AssignAgv> context)
        {
            _logger.LogInformation("Order {OrderId} / {CorrelationId} requests assignment to an AGV", context.Message.Order.OrderId, context.Message.CorrelationId);

            KeyValuePair<IAgv, IGraphAlgorithm.Route>? assignAgvAndRoute = await AssignIdleAgv<AStar>(context.Message.Order, context.CancellationToken);
            if (assignAgvAndRoute == null)
            {
                var error = CheckRouteExists<AStar>(context.Message.Order) ? AgvAssignmentErrorCode.NoIdleAgv : AgvAssignmentErrorCode.NoRouteFound;
                await context.Publish(new AgvAssignmentFailed(context.Message.CorrelationId, error));
            }
            else
            {
                var agv = assignAgvAndRoute.Value.Key;
                var route = assignAgvAndRoute.Value.Value;

                await context.Publish(new AgvAssignmentSucceeded(
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
        private async Task<KeyValuePair<IAgv, IGraphAlgorithm.Route>?> AssignIdleAgv<TGraphAlgorithm>(OrderDetails order, CancellationToken cancellationToken) where TGraphAlgorithm : IGraphAlgorithm
        {

            var agvChooser = new AgvChooser<TGraphAlgorithm>();
            IAgv? assignedAgv = null;
            IGraphAlgorithm.Route? route = null;

            List<IAgv> idleAgvs = _agvRegistry.GetIdleAgvs()
                .Where(agv => agv.AgvGraph != null)
                .ToList();

            if (idleAgvs.Any(agv => agv.AgvGraph!.Usability != AgvGraph.GraphUsability.Valid))
            {
                _logger.LogInformation("There are AGVs with invalid graphs.");
                idleAgvs.RemoveAll(x => x.AgvGraph!.Usability != AgvGraph.GraphUsability.Valid);
            }

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
        private bool CheckRouteExists<TGraphAlgorithm>(OrderDetails order) where TGraphAlgorithm : IGraphAlgorithm
        {
            var agvChooser = new AgvChooser<TGraphAlgorithm>();
            IEnumerable<IAgv> agvs = _agvRegistry.GetAllAgvs()
                .Where(agv => agv.AgvGraph != null);

            return agvs.Any(agv => agvChooser.CalculateRoute(agv, order) != null);
        }

        /// <summary>
        /// Respond to <see cref="AgvBecameIdle"/> message
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<AgvBecameIdle> context)
        {
            var agvChooser = new AgvChooser<AStar>();
            var agv = _agvRegistry.GetAgv(context.Message.CorrelationId);
            if (agv == null)
            {
                _logger.LogError("AGV {CorrelationId} became idle, but no AGV with this correlation ID can be found", context.Message.CorrelationId);
                return;
            }

            //1. Get unassigned Orders
            List<IOrder> unassignedOrders = _orderRegistry.GetUnassignedOrders().ToList();
            if (unassignedOrders.Count == 0)
            {
                return;
            }

            //1. Get open Orders
            List<IOrder> openOrders = _orderRegistry.GetUnassignedOrders().ToList();
            IOrder? assignedOrder = null;
            foreach (var order in openOrders)
            {
                _logger.LogInformation("Trying to assign order {OrderId} / {CorrelationId} in state {State}", order.OrderDetails.OrderId, order.CorrelationId, order.CurrentState);

                //2. See if agv has a route for this order
                if (agvChooser.CalculateRoute(agv, order.OrderDetails) != null)
                {
                    //3. Try to assign
                    await context.Publish(new RequestOrderAgvAssign(order.CorrelationId));
                    assignedOrder = order;
                    break;
                }
                else
                {
                    _logger.LogInformation("Assign order {OrderId} / {CorrelationId} in state {State} failed because AGV can't find a route", order.OrderDetails.OrderId, order.CorrelationId, order.CurrentState);
                }
                //4. Try next order
                unassignedOrders.RemoveAll(x => x.CorrelationId == order.CorrelationId);
            }

            if (assignedOrder != null)
            {
                _logger.LogInformation("AGV {AgvId} / {CorrelationId} became idle and assignment for order {OrderId}/{OrderCorrelationId} has started", agv.AgvId, context.Message.CorrelationId, assignedOrder.OrderDetails.OrderId, assignedOrder.CorrelationId);
            }
            else
            {
                _logger.LogInformation("AGV {AgvId} / {CorrelationId} became idle but open order(s) cannot be assigned, because AGV does not have a route for order(s)", agv.AgvId, context.Message.CorrelationId);
            }
        }
    }
}
