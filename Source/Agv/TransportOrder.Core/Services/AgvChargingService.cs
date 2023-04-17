using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations.Agvs;

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
        private readonly ILogger<AgvChargingService> _logger;
        private readonly IAgvRegistry _agvRegistry;
        private readonly IOrderRegistry _orderRegistry;
        private readonly IAgvSettingsProvider _settingsProvider;
        private readonly IGraphRegistry _graphRegistry;

        /// <summary>
        /// Create a new instance of <see cref="AgvChargingService"/>
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="agvRegistry"></param>
        /// <param name="orderRegistry"></param>
        /// <param name="graphRegistry"></param>
        /// <param name="settingsProvider"></param>
        public AgvChargingService(
            ILogger<AgvChargingService> logger,
            IAgvRegistry agvRegistry,
            IOrderRegistry orderRegistry,
            IGraphRegistry graphRegistry,
            IAgvSettingsProvider settingsProvider
            )
        {
            _logger = logger;
            _agvRegistry = agvRegistry;
            _orderRegistry = orderRegistry;
            _graphRegistry = graphRegistry;
            _settingsProvider = settingsProvider;
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
        /// Handle AgvStatusReceived event
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<AgvStatusReceived> context)
        {
            var agvStatus = context.Message.AgvStatus;
            var agvSettings = _settingsProvider.GetSettingsForAgv(agvStatus.CorrelationId);

            if (agvStatus.Charging)
            {
                var agv = _agvRegistry.GetAgv(context.Message.CorrelationId);
                if (agv == null)
                {
                    _logger.LogWarning("Agv with correlationId {CorrelationId} doesn't exist", context.Message.CorrelationId);
                    return;
                }

                if (agv.CurrentState != nameof(AgvStateMachine.Charging))
                {
                    return; // AgvStoppedCharging message was already send
                }

                if (agvStatus.BatteryCharge >= agvSettings.BatteryLevelFull)
                {
                    _logger.LogInformation("Agv with correlationId {CorrelationId} reached full battery level ({BatteryCharge}%)", agvStatus.CorrelationId, agvStatus.BatteryCharge);
                    await context.Publish(new AgvStoppedCharging(agvStatus.CorrelationId));
                }
                else if (agvStatus.BatteryCharge >= agvSettings.BatteryLevelGood && _orderRegistry.CountQueued > 0)
                {
                    _logger.LogInformation("Agv with correlationId {CorrelationId} reached good battery level ({BatteryCharge}%)", agvStatus.CorrelationId, agvStatus.BatteryCharge);
                    await context.Publish(new AgvStoppedCharging(agvStatus.CorrelationId));
                }
            }
            else // Check battery charge of active AGV
            {
                if (CheckAgvNeedsCharging(agvStatus, agvSettings))
                {
                    await context.Publish(new AgvNeedsCharging(agvStatus.CorrelationId)); // This is a placeholder, there are currently no consumers for this message
                }
            }
        }

        private bool CheckAgvNeedsCharging(AgvStatus agvStatus, AgvSettings agvSettings)
        {
            if (agvStatus.BatteryCharge <= agvSettings.BatteryLevelCritical)
            {
                _logger.LogInformation("Agv with correlationId {CorrelationId} has critical battery level ({BatteryCharge}%)", agvStatus.CorrelationId, agvStatus.BatteryCharge);
                return true;
            }

            if (agvStatus.BatteryCharge <= agvSettings.BatteryLevelLow)
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

        private bool CheckAgvNeedsCharging(AgvStatus agvStatus)
        {
            var agvSettings = _settingsProvider.GetSettingsForAgv(agvStatus.CorrelationId);
            return CheckAgvNeedsCharging(agvStatus, agvSettings);
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
                destination);

            agvAction.AssignedAgv = agv.CorrelationId;
            agvAction.Nodes = route.Nodes;

            await context.Publish(new StartActionCommand(agv.CorrelationId, agvAction));
            await context.Publish(new ActionStartedEvent(agv.CorrelationId, string.Empty));
        }

        private AgvGraph.Node? ChooseChargingStation(AgvGraph agvGraph)
        {
            List<AgvGraph.Node> chargingNodes = agvGraph.Nodes.Where(n => n.Type == AgvGraph.NodeType.Charge).ToList();

            if (chargingNodes.Count == 0)
            {
                return null; // AGV has no charging nodes in its graph
            }

            foreach (var node in chargingNodes)
            {
                // Try reserving node
                if (_graphRegistry.ReserveNode(node.Id))
                {
                    return node; // Return reserved charging node
                }
            }

            return null; // Charging node could not be reserved
        }
    }
}
