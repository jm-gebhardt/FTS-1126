using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Actions.Charging
{
    public class AgvBatteryChangedConsumerTestFixture: MassTransitTestFixture<AgvBatteryChangedConsumer>
    {
        private readonly Mock<ILogger> _logger = new ();
        
        public const int BatteryLevelCritical = 10;
        public const int BatteryLevelLow = 40;
        public const int BatteryLevelGood = 80;
        public const int BatteryLevelFull = 100;
        
        private static readonly AgvSettings _agvSettings = new()
        {
            BatteryLevelCritical = BatteryLevelCritical,
            BatteryLevelFull = BatteryLevelFull,
            BatteryLevelGood = BatteryLevelGood,
            BatteryLevelLow = BatteryLevelLow,
            AgvOfflineTimeoutMsWhileIdle = 15000,
            AgvOfflineTimeoutMsWhileBusy = 20000
        };

        protected override void ConfigureServices(IServiceCollection collection)
        {
            collection.AddTransient(_ => _logger);
            collection.AddAgvSettings(_agvSettings);
        }
    }
}


