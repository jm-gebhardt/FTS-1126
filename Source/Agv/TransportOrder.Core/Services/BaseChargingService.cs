using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations.Agvs;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// Base Implementation of IChargingService
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class BaseAgvChargingService : IChargingService, IConsumer<AgvBatteryChargeChanged>
    {
        private readonly ILogger<BaseAgvChargingService> _logger;
        private readonly IAgvRegistry _agvRegistry;
        private readonly IAgvSettingsProvider _settingsProvider;

        /// <summary>
        /// Create a new instance of <see cref="BaseAgvChargingService"/>
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="agvRegistry"></param>
        /// <param name="settingsProvider"></param>
        public BaseAgvChargingService(
            ILogger<BaseAgvChargingService> logger,
            IAgvRegistry agvRegistry,
            IAgvSettingsProvider settingsProvider)
        {
            _logger = logger;
            _agvRegistry = agvRegistry;
            _settingsProvider = settingsProvider;
        }

        /// <summary>
        /// Handle AgvStatusReceived event
        /// </summary>
        public Task Consume(ConsumeContext<AgvBatteryChargeChanged> context)
        {
            var agvSettings = _settingsProvider.GetSettingsForAgv(context.Message.CorrelationId);

            bool oldCharged = context.Message.OldBatteryCharge > agvSettings.BatteryLevelLow;
            bool newCharged = context.Message.NewBatteryCharge > agvSettings.BatteryLevelLow;

            // Check if agv became charged
            if (!oldCharged && newCharged)
            {
                NotifyChargingAgvsCanHandleActionsAgain(context);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public bool AgvNeedsCharging(IAgv agv)
        {
            if (agv.AgvStatus == null)
            {
                _logger.LogWarning("Agv with correlationId {CorrelationId} has no Status", agv.CorrelationId);
                return false;
            }

            var agvSettings = _settingsProvider.GetSettingsForAgv(agv.CorrelationId);

            return agv.AgvStatus.BatteryCharge <= agvSettings.BatteryLevelCritical;
        }

        /// <inheritdoc/>
        public bool AgvIsEligibleForCharging(IAgv agv)
        {
            if (agv.AgvStatus == null)
            {
                _logger.LogWarning("Agv with correlationId {CorrelationId} has no Status", agv.CorrelationId);
                return false;
            }

            var agvSettings = _settingsProvider.GetSettingsForAgv(agv.CorrelationId);

            return agv.AgvStatus.BatteryCharge <= agvSettings.BatteryLevelLow;
        }

        /// <inheritdoc/>
        public List<IAgv> GetChargingAgvsWithGoodBatteryLevel()
        {
            return _agvRegistry.GetAllAgvs()
                .Where(agv => agv.AgvStatus != null && agv.AgvStatus.BatteryCharge > _settingsProvider.GetSettingsForAgv(agv.CorrelationId).BatteryLevelLow)
                .ToList();
        }

        /// <inheritdoc/>
        public void NotifyChargingAgvsCanHandleActionsAgain(IPublishEndpoint publishEndpoint)
        {
            publishEndpoint.Publish(new ChargingAgvsCanHandleActionsAgainEvent());
        }
    }
}