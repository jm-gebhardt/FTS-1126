using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// Class for checking the battery level of an AGV
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AgvChargingService :
        IConsumer<AgvBecameIdle>,
        IConsumer<AgvStatusReceived>
    {
        /// <summary>Critical battery level</summary>
        public const int BatteryLevelCritical = 10;

        /// <summary>Low battery level</summary>
        public const int BatteryLevelLow = 40;

        /// <summary>Good battery level</summary>
        public const int BatteryLevelGood = 80;

        /// <summary>Full battery level</summary>
        public const int BatteryLevelFull = 100;

        private readonly ILogger<AgvChargingService> _logger;
        private readonly IAgvRegistry _agvRegistry;
        private readonly IOrderRegistry _orderRegistry;

        /// <summary>
        /// Create a new instance of <see cref="AgvChargingService"/>
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="agvRegistry"></param>
        /// <param name="orderRegistry"></param>
        public AgvChargingService(ILogger<AgvChargingService> logger, IAgvRegistry agvRegistry, IOrderRegistry orderRegistry)
        {
            _logger = logger;
            _agvRegistry = agvRegistry;
            _orderRegistry = orderRegistry;
        }

        /// <summary>
        /// Handle AgvBecameIdle event
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<AgvBecameIdle> context)
        {
            var agv = _agvRegistry.GetAgv(context.Message.CorrelationId);
            if (agv == null)
            {
                _logger.LogWarning("Agv with correlationId {CorrelationId} doesn't exist", context.Message.CorrelationId);
                return;
            }

            if (agv.AgvStatus == null)
            {
                _logger.LogWarning("Agv with correlationId {CorrelationId} has no Status", agv.CorrelationId);
                return;
            }

            if (CheckAgvNeedsCharging(agv.AgvStatus))
            {
                await CreateChargingAction(agv, context);
            }
            else
            {
                await context.Publish(new AgvReady(agv.CorrelationId));
            }
        }

        /// <summary>
        /// Handle AgvBecameIdle event
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<AgvStatusReceived> context)
        {
            var agvStatus = context.Message.AgvStatus;

            if (agvStatus.Charging)
            {
                if (agvStatus.BatteryCharge >= BatteryLevelFull)
                {
                    _logger.LogInformation("Agv with correlationId {CorrelationId} reached full battery level ({BatteryCharge}%)", agvStatus.CorrelationId, agvStatus.BatteryCharge);
                    await context.Publish(new AgvStoppedCharging(agvStatus.CorrelationId));
                }
                else if (agvStatus.BatteryCharge >= BatteryLevelGood && _orderRegistry.CountQueued > 0)
                {
                    _logger.LogInformation("Agv with correlationId {CorrelationId} reached good battery level ({BatteryCharge}%)", agvStatus.CorrelationId, agvStatus.BatteryCharge);
                    await context.Publish(new AgvStoppedCharging(agvStatus.CorrelationId));
                }
            }
            else // Check battery charge of active AGV
            {
                if (CheckAgvNeedsCharging(agvStatus))
                {
                    await context.Publish(new AgvNeedsCharging(agvStatus.CorrelationId)); // This is a placeholder, there are currently no consumers for this message
                }
            }
        }

        private bool CheckAgvNeedsCharging(AgvStatus agvStatus)
        {
            if (agvStatus.BatteryCharge <= BatteryLevelCritical)
            {
                _logger.LogInformation("Agv with correlationId {CorrelationId} has critical battery level ({BatteryCharge}%)", agvStatus.CorrelationId, agvStatus.BatteryCharge);
                return true;
            }

            if (agvStatus.BatteryCharge <= BatteryLevelLow)
            {
                _logger.LogInformation("Agv with correlationId {CorrelationId} has low battery level ({BatteryCharge}%)", agvStatus.CorrelationId, agvStatus.BatteryCharge);

                if (_orderRegistry.CountQueued == 0) // No orders -> send AGV to charging station
                {
                    return true;
                }
                return false;
            }

            // agvStatus.BatteryCharge > BatteryLevelLow
            _logger.LogDebug("Agv with correlationId {CorrelationId} has good battery level ({BatteryCharge}%)", agvStatus.CorrelationId, agvStatus.BatteryCharge);
            return false;
        }

        private async Task CreateChargingAction(IAgv agv, ConsumeContext context)
        {
            _logger.LogInformation("Start action {ActionType} for AGV {AgvId} / {CorrelationId}",
                ActionType.Charge, agv.AgvId, agv.CorrelationId);

            if (agv.AgvGraph == null)
            {
                _logger.LogWarning("Agv with correlationId {CorrelationId} has no graph", agv.CorrelationId);
                return;
            }

            var chargingNode = ChooseChargingStation(agv.AgvGraph);
            if (chargingNode == null)
            {
                _logger.LogWarning("Agv with correlationId {CorrelationId} has no charging stations in its graph", agv.CorrelationId);
                return;
            }

            string source = agv.AgvStatus!.LastNodeId ?? "";
            string destination = chargingNode.Id;

            var graphAlgorithm = new AStar(agv.AgvGraph, new NoCheckPlausibilityChecker());

            var route = graphAlgorithm.CalculateRoute(source, destination);
            if (route == null)
            {
                _logger.LogWarning("Charging station {NodeId} is not reachable by AGV with correlationId {CorrelationId}", source, agv.CorrelationId);
                return;
            }
            // ReSharper disable once UseObjectOrCollectionInitializer
            var agvAction = new AgvAction(
                Guid.NewGuid(),
                agv.AgvId,
                ActionType.Charge,
                string.Empty,
                Guid.Empty,
                source,
                destination);

            agvAction.AgvCorrelationId = agv.CorrelationId;
            agvAction.Nodes = route.Nodes;

            await context.Publish(new StartActionCommand(agv.CorrelationId, agvAction));
            await context.Publish(new ActionStartedEvent(agv.CorrelationId));
        }

        private AgvGraph.Node? ChooseChargingStation(AgvGraph agvGraph)
        {
            List<AgvGraph.Node> chargingNodes = agvGraph.Nodes.Where(n => n.Type == AgvGraph.NodeType.Charge).ToList();

            if (chargingNodes.Count == 0)
            {
                return null;
            }

            var random = new Random(); // Get random charging node
            return chargingNodes[random.Next(chargingNodes.Count)];
        }
    }
}
