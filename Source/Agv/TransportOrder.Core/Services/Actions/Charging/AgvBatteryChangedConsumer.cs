using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Settings;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging
{
    /// <summary>
    /// 
    /// </summary>
    [UsedImplicitly]
    public class AgvBatteryChangedConsumer: IConsumer<AgvBatteryChargeChanged>
    {
        private readonly ISettingsProvider<AgvSettings> _settingsProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settingsProvider"></param>
        public AgvBatteryChangedConsumer(ISettingsProvider<AgvSettings> settingsProvider)
        {
            _settingsProvider = settingsProvider;
        }
        /// <summary>
        /// Handle AgvStatusReceived event
        /// </summary>
        public Task Consume(ConsumeContext<AgvBatteryChargeChanged> context)
        {
            var agvSettings = _settingsProvider.GetSettings();

            double oldSoc = context.Message.OldBatteryCharge;
            double newSoc = context.Message.NewBatteryCharge;

            bool isCharging = oldSoc < newSoc;
            bool wasGood = oldSoc > agvSettings.BatteryLevelLow;
            bool isGood = newSoc > agvSettings.BatteryLevelLow;

            // Check if AGV is charging and no longer low on battery
            if (!wasGood && isGood)
            {
                NotifyChargingAgvsCanHandleActionsAgain(context);
            }

            // Battery needs charging
            if (!isCharging && !isGood)
            {
                context.Publish(new AgvNeedsCharging(context.Message.CorrelationId));
            }

            return Task.CompletedTask;
        }
        
        private void NotifyChargingAgvsCanHandleActionsAgain(IPublishEndpoint publishEndpoint)
        {
            publishEndpoint.Publish(new ChargingAgvsCanHandleActionsAgainEvent());
        }
    }
}