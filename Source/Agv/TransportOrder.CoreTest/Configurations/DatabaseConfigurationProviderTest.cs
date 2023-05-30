using Gebhardt.StoreWare.Agv.Common.Configurations.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Configurations
{
    [TestClass]
    public class DatabaseConfigurationProviderTest
    {
        [TestMethod]
        public async Task ConfigurationSettings_ShouldPopulateDefaultConfigurationData_WhenUsingDatabaseConfigurationProvider()
        {
            var testFixture = new ConfigurationProviderTestFixture()
                .AddSettingsProvider<ConfigurationProviderTestFixture.TestSettings>()
                .AddDefaultConfigurationProvider()
                .AddDatabase()
                .AddDatabaseConfigurationProvider()
                .AddSeeder()
                .Build();

            await testFixture.SeedConfigurationTypesAsync();

            IList<ConfigurationSetting> configurationSettings = await testFixture.GetConfigurationSettingsAsync();
            configurationSettings.ShouldNotBeEmpty();

            foreach (var configurationType in ConfigurationProviderTestFixture.ConfigurationTypes.Where(t => t.AutoSeedToDatabase))
            {
                List<ConfigurationSetting> settings = configurationSettings.Where(cf => cf.ConfigurationType == configurationType.Name).ToList();
                if (!settings.Any())
                {
                    throw new Exception($"Empty settings: {configurationType.Name}");
                }
                settings.ShouldNotBeEmpty();

                foreach (var configurationTypeProperty in configurationType.Properties.Where(p => p.AutoSeedToDatabase))
                {
                    string key = configurationTypeProperty.Key;
                    var setting = settings.FirstOrDefault(s => s.Key == key);
                    setting.ShouldNotBeNull();
                    setting.Key.ShouldBe(key);

                    if (string.IsNullOrEmpty(configurationTypeProperty.Value))
                    {
                        setting.Value.ShouldBeNullOrEmpty();
                    }
                    else
                    {
                        setting.Value.ShouldBe(configurationTypeProperty.Value);
                    }

                    setting.ConfigurationType.ShouldBe(configurationType.Name);
                }
            }
        }
    }
}