using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

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
            _orderRegistry=orderRegistry;
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
            }
            else if (agv.AgvStatus == null)
            {
                _logger.LogWarning("Agv with correlationId {CorrelationId} has no Status", context.Message.CorrelationId);
            }
            else
            {
                await CheckAgvBatteryLevel(agv.AgvStatus, context);
            }
        }

        /// <summary>
        /// Handle AgvBecameIdle event
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<AgvStatusReceived> context)
        {
            await CheckAgvChargingCompleted(context.Message.AgvStatus, context);
        }

        /// <summary>
        /// Check the battery level of an AGV
        /// </summary>
        /// <param name="agvStatus">Status of the AGV</param>
        /// <param name="endpoint">MassTransit endpoint for publishing a message</param>
        private async Task CheckAgvBatteryLevel(AgvStatus agvStatus, IPublishEndpoint endpoint)
        {
            if (agvStatus.BatteryCharge <= BatteryLevelCritical)
            {
                _logger.LogInformation("Agv with correlationId {CorrelationId} has critical battery level ({BatteryCharge}%)", agvStatus.CorrelationId, agvStatus.BatteryCharge);
                await endpoint.Publish(new AgvNeedsCharging(agvStatus.CorrelationId));
            }
            else if (agvStatus.BatteryCharge <= BatteryLevelLow)
            {
                _logger.LogInformation("Agv with correlationId {CorrelationId} has low battery level ({BatteryCharge}%)", agvStatus.CorrelationId, agvStatus.BatteryCharge);
                await endpoint.Publish(new AgvRequestsCharging(agvStatus.CorrelationId));
            }
            else
            {
                _logger.LogInformation("Agv with correlationId {CorrelationId} has good battery level ({BatteryCharge}%)", agvStatus.CorrelationId, agvStatus.BatteryCharge);
                await endpoint.Publish(new AgvReady(agvStatus.CorrelationId));
            }
        }

        /// <summary>
        /// Checks if the battery level of an AGV is high enough to stop charging
        /// </summary>
        /// <param name="agvStatus">Status of the AGV</param>
        /// <param name="endpoint">MassTransit endpoint for publishing a message</param>
        private async Task CheckAgvChargingCompleted(AgvStatus agvStatus, IPublishEndpoint endpoint)
        {
            int cntQueuedOrder = _orderRegistry.CountQueued;

            // ReSharper disable once MergeIntoPattern
            if (agvStatus.Charging && agvStatus.BatteryCharge >= BatteryLevelGood && cntQueuedOrder > 0)
            {
                _logger.LogInformation("Agv with correlationId {CorrelationId} reached good battery level ({BatteryCharge}%)", agvStatus.CorrelationId, agvStatus.BatteryCharge);
                await endpoint.Publish(new AgvStoppedCharging(agvStatus.CorrelationId));
            }
            // ReSharper disable once MergeIntoPattern
            else if (agvStatus.Charging && agvStatus.BatteryCharge >= BatteryLevelFull)
            {
                _logger.LogInformation("Agv with correlationId {CorrelationId} reached full battery level ({BatteryCharge}%)", agvStatus.CorrelationId, agvStatus.BatteryCharge);
                await endpoint.Publish(new AgvStoppedCharging(agvStatus.CorrelationId));
            }
        }
    }
}
