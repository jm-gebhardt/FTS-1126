using Gebhardt.StoreWare.Agv.Common.Configurations.Models;
using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using static Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Configurations.ConfigurationProviderTestFixture;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Configurations
{
    [TestClass]
    public class SettingsProviderTest
    {
        [TestMethod]
        public void GetSettings_ShouldReturnDefaultAgvSettings_WhenUsingDefaultConfigurationProvider()
        {
            var testFixture = new ConfigurationProviderTestFixture()
                .AddSettingsProvider<AgvSettings>()
                .AddDefaultConfigurationProvider()
                .Build();

            ISettingsProvider<AgvSettings> settingsProvider = testFixture.GetSettingsProvider<AgvSettings>();

            var expectedAgvSettings = new AgvSettings();
            var actualAgvSettings = settingsProvider.GetSettings();

            actualAgvSettings.ShouldNotBeNull();
            actualAgvSettings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(expectedAgvSettings.AgvOfflineTimeoutMsWhileBusy);
            actualAgvSettings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(expectedAgvSettings.AgvOfflineTimeoutMsWhileIdle);
        }

        [TestMethod]
        public async Task GetSettings_ShouldReturnDatabaseAgvSettings_WhenUsingDatabaseConfigurationProvider()
        {
            var testFixture = new ConfigurationProviderTestFixture()
                .AddSettingsProvider<AgvSettings>()
                .AddDbSetSagaDbContext()
                .AddDatabaseConfigurationProvider()
                .Build();

            IDbContextFactory<DbSetSagaPersistenceDbContext> dbContextFactory = testFixture.DbSetSagaPersistenceDbContextFactory;
            await using var ctx = await dbContextFactory.CreateDbContextAsync();
            string agvOfflineTimeoutMsWhileBusyKey = $"{nameof(AgvSettings)}:{nameof(AgvSettings.AgvOfflineTimeoutMsWhileBusy)}";

            await UpdateConfigurationSettingAsync(ctx, agvOfflineTimeoutMsWhileBusyKey, "123", nameof(AgvSettings));

            ISettingsProvider<AgvSettings> settingsProvider = testFixture.GetSettingsProvider<AgvSettings>();

            var startedTime = DateTime.Now;
            var timeout = TimeSpan.FromMilliseconds(1000);
            bool exist;
            do
            {
                exist = settingsProvider.DoTheSettingsExist();
            } while (!exist && DateTime.Now < startedTime + timeout);
            exist.ShouldBeTrue();

            var agvSettings = settingsProvider.GetSettings();
            agvSettings.ShouldNotBeNull();
            agvSettings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(123);
        }

        private static async Task UpdateConfigurationSettingAsync(DbSetSagaPersistenceDbContext ctx, string key, string value, string configurationType)
        {
            var configuration = ctx.ConfigurationSettings.FirstOrDefault(c => c.Key == key);

            if (configuration == null)
            {
                await ctx.ConfigurationSettings.AddAsync(new ConfigurationSetting { Key = key, Value = value, ConfigurationType = configurationType });
            }
            else
            {
                configuration.Value = value;
                ctx.ConfigurationSettings.Update(configuration);
            }

            await ctx.SaveChangesAsync();
        }
    }
}