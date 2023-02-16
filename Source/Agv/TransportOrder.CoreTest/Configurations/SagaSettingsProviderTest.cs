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
        [TestMethod]
        public async Task GetSettings_ShouldReturnAgvSettingsOfAgvSaga_WhenUsingSagaSettingsProvider()
        {
            int agvOfflineTimeoutMsWhileBusy = 1000;
            int agvOfflineTimeoutMsWhileIdle = 2000;

            var testFixture = new ConfigurationProviderTestFixture()
                            .AddSettingsProvider<AgvSettings>()
                            .AddSagaSettingsProvider<AgvSettings, AgvSaga>()
                            .AddDbSetSagaDbContext()
                            .AddDefaultConfigurationProvider()
                            .Build();

            SagaSettingsProvider<AgvSettings, AgvSaga, ConfigurationProviderTestFixture.DbSetSagaPersistenceDbContext> sagaSettingsProvider = testFixture.GetSagaSettingsProvider<AgvSettings, AgvSaga>();

            testFixture.AddAgvSagaSettings(agvOfflineTimeoutMsWhileBusy, agvOfflineTimeoutMsWhileIdle);
            await testFixture.CreateAgvSagaAsync();
            var restoredAgvSaga = await testFixture.GetAgvSagaAsync();

            var agvSettings = sagaSettingsProvider.GetSettings(restoredAgvSaga.CorrelationId);

            agvSettings.ShouldNotBeNull();
            agvSettings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(agvOfflineTimeoutMsWhileBusy);
            agvSettings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(agvOfflineTimeoutMsWhileIdle);
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
            var restoredAgvSaga = await testFixture.GetAgvSagaAsync();

            var agvSagaSettings = sagaSettingsProvider.GetSettings(restoredAgvSaga.CorrelationId);
            var defaultAgvSettings = settingsProvider.GetSettings();

            agvSagaSettings.ShouldNotBeNull();
            defaultAgvSettings.ShouldNotBeNull();
            agvSagaSettings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(defaultAgvSettings.AgvOfflineTimeoutMsWhileBusy);
            agvSagaSettings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(defaultAgvSettings.AgvOfflineTimeoutMsWhileIdle);
        }

    }
}