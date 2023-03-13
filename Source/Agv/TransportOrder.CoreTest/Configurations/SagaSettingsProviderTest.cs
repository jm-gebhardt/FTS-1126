using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Configurations
{
    [TestClass]
    public class SagaSettingsProviderTest
    {
        private const int AgvOfflineTimeoutMsWhileBusy = 1111;
        private const int AgvOfflineTimeoutMsWhileIdle = 2222;
        private const float OccupiedDistanceThreshold = 1.0f;
        private const int BatteryLevelFull = 99;
        private const int BatteryLevelGood = 55;
        private const int BatteryLevelLow = 44;
        private const int BatteryLevelCritical = 33;
        private static AgvSagaSettings GetSagaSettings() => new()
        {
            AgvOfflineTimeoutMsWhileBusy = AgvOfflineTimeoutMsWhileBusy,
            AgvOfflineTimeoutMsWhileIdle = AgvOfflineTimeoutMsWhileIdle,
            BatteryLevelCritical = BatteryLevelCritical,
            BatteryLevelLow = BatteryLevelLow,
            BatteryLevelFull = BatteryLevelFull,
            BatteryLevelGood = BatteryLevelGood
        };

        [TestMethod]
        public async Task GetSettings_ShouldReturnAgvSettingsOfAgvSaga_WhenUsingSagaSettingsProvider()
        {
            var testFixture = new ConfigurationProviderTestFixture()
                            .AddSettingsProvider<AgvSettings>()
                            .AddSagaSettingsProvider<AgvSettings, AgvSaga>()
                            .AddDbSetSagaDbContext()
                            .AddDefaultConfigurationProvider()
                            .Build();

            SagaSettingsProvider<AgvSettings, AgvSaga, ConfigurationProviderTestFixture.DbSetSagaPersistenceDbContext> sagaSettingsProvider = testFixture.GetSagaSettingsProvider<AgvSettings, AgvSaga>();

            testFixture.AddAgvSagaSettings(GetSagaSettings());
            await testFixture.CreateAgvSagaAsync();

            var agvSettings = sagaSettingsProvider.GetSettings(testFixture.CorrelationId);

            agvSettings.ShouldNotBeNull();
            agvSettings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(AgvOfflineTimeoutMsWhileBusy);
            agvSettings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(AgvOfflineTimeoutMsWhileIdle);
            agvSettings.OccupiedDistanceThreshold.ShouldBe(OccupiedDistanceThreshold);
            agvSettings.BatteryLevelCritical.ShouldBe(BatteryLevelCritical);
            agvSettings.BatteryLevelFull.ShouldBe(BatteryLevelFull);
            agvSettings.BatteryLevelGood.ShouldBe(BatteryLevelGood);
            agvSettings.BatteryLevelLow.ShouldBe(BatteryLevelLow);
        }

        [TestMethod]
        public async Task GetSettings_ShouldReturnDefaultAgvSettings_WhenAgvSagaSettingsIsNull()
        {
            var testFixture = new ConfigurationProviderTestFixture()
                            .AddSettingsProvider<AgvSettings>()
                            .AddSagaSettingsProvider<AgvSettings, AgvSaga>()
                            .AddDbSetSagaDbContext()
                            .AddDefaultConfigurationProvider()
                            .Build();

            SagaSettingsProvider<AgvSettings, AgvSaga, ConfigurationProviderTestFixture.DbSetSagaPersistenceDbContext> sagaSettingsProvider = testFixture.GetSagaSettingsProvider<AgvSettings, AgvSaga>();
            ISettingsProvider<AgvSettings> settingsProvider = testFixture.GetSettingsProvider<AgvSettings>();

            await testFixture.CreateAgvSagaAsync();

            var agvSagaSettings = sagaSettingsProvider.GetSettings(testFixture.CorrelationId);
            var defaultAgvSettings = settingsProvider.GetSettings();

            agvSagaSettings.ShouldNotBeNull();
            defaultAgvSettings.ShouldNotBeNull();
            agvSagaSettings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(defaultAgvSettings.AgvOfflineTimeoutMsWhileBusy);
            agvSagaSettings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(defaultAgvSettings.AgvOfflineTimeoutMsWhileIdle);
            agvSagaSettings.OccupiedDistanceThreshold.ShouldBe(defaultAgvSettings.OccupiedDistanceThreshold);
            agvSagaSettings.BatteryLevelCritical.ShouldBe(defaultAgvSettings.BatteryLevelCritical);
            agvSagaSettings.BatteryLevelFull.ShouldBe(defaultAgvSettings.BatteryLevelFull);
            agvSagaSettings.BatteryLevelGood.ShouldBe(defaultAgvSettings.BatteryLevelGood);
            agvSagaSettings.BatteryLevelLow.ShouldBe(defaultAgvSettings.BatteryLevelLow);
        }


        [TestMethod]
        [DataRow(AgvOfflineTimeoutMsWhileBusy, AgvOfflineTimeoutMsWhileIdle, BatteryLevelCritical, BatteryLevelLow, BatteryLevelGood, BatteryLevelFull)]
        [DataRow(null, null, null, null, null, null)]
        [DataRow(AgvOfflineTimeoutMsWhileBusy, AgvOfflineTimeoutMsWhileIdle, BatteryLevelCritical, null, null, BatteryLevelFull)]
        public async Task GetSettings_UsesSpecificValuesFromSagaOnAPropertyLevel(int? agvOfflineTimeoutMsWhileBusy,
            int? agvOfflineTimeoutMsWhileIdle,
            int? batteryLevelCritical,
            int? batteryLevelLow,
            int? batteryLevelGood,
            int? batteryLevelFull)
        {
            var testFixture = new ConfigurationProviderTestFixture()
                            .AddSettingsProvider<AgvSettings>()
                            .AddSagaSettingsProvider<AgvSettings, AgvSaga>()
                            .AddDbSetSagaDbContext()
                            .AddDefaultConfigurationProvider()
                            .Build();

            SagaSettingsProvider<AgvSettings, AgvSaga, ConfigurationProviderTestFixture.DbSetSagaPersistenceDbContext> sagaSettingsProvider = testFixture.GetSagaSettingsProvider<AgvSettings, AgvSaga>();

            testFixture.AddAgvSagaSettings(new()
            {
                AgvOfflineTimeoutMsWhileBusy = agvOfflineTimeoutMsWhileBusy,
                AgvOfflineTimeoutMsWhileIdle = agvOfflineTimeoutMsWhileIdle,
                BatteryLevelCritical = batteryLevelCritical,
                BatteryLevelLow = batteryLevelLow,
                BatteryLevelFull = batteryLevelFull,
                BatteryLevelGood = batteryLevelGood
            });

            await testFixture.CreateAgvSagaAsync();

            ISettingsProvider<AgvSettings> settingsProvider = testFixture.GetSettingsProvider<AgvSettings>();
            var settings = sagaSettingsProvider.GetSettings(testFixture.CorrelationId);
            var defaultSettings = settingsProvider.GetSettings();

            settings.ShouldNotBeNull();
            settings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(agvOfflineTimeoutMsWhileBusy ?? defaultSettings.AgvOfflineTimeoutMsWhileBusy);
            settings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(agvOfflineTimeoutMsWhileIdle ?? defaultSettings.AgvOfflineTimeoutMsWhileIdle);
            settings.BatteryLevelCritical.ShouldBe(batteryLevelCritical ?? defaultSettings.BatteryLevelCritical);
            settings.BatteryLevelFull.ShouldBe(batteryLevelFull ?? defaultSettings.BatteryLevelFull);
            settings.BatteryLevelGood.ShouldBe(batteryLevelGood ?? defaultSettings.BatteryLevelGood);
            settings.BatteryLevelLow.ShouldBe(batteryLevelLow ?? defaultSettings.BatteryLevelLow);
        }
    }
}