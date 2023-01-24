using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// Class for checking the battery level of an AGV
    /// </summary>
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

        private readonly ILogger<AgvChargingService> _logger;
        private readonly IAgvRegistry _agvRegistry;

        /// <summary>
        /// Create a new instance of <see cref="AgvChargingService"/>
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="agvRegistry"></param>
        public AgvChargingService(ILogger<AgvChargingService> logger, IAgvRegistry agvRegistry)
        {
            _logger = logger;
            _agvRegistry = agvRegistry;
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
            // ReSharper disable once MergeIntoPattern
            if (agvStatus.Charging && agvStatus.BatteryCharge >= BatteryLevelGood)
            {
                _logger.LogInformation("Agv with correlationId {CorrelationId} reached good battery level ({BatteryCharge}%)", agvStatus.CorrelationId, agvStatus.BatteryCharge);
                await endpoint.Publish(new AgvReady(agvStatus.CorrelationId));
            }
        }
    }
}
