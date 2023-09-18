using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Configurations;

[TestClass]
public class DefaultConfigurationProviderTest
{
    [TestMethod]
    public void SettingsValue_ShouldGetFromConfiguration_WhenUsingDefaultConfigurationProvider()
    {
        var testFixture = new ConfigurationProviderTestFixture()
            .AddDefaultConfigurationProvider()
            .Build();
                    
        foreach (var configurationType in testFixture.ConfigurationTypes)
        {
            foreach (var configurationTypeProperty in configurationType.Properties)
            {
                string? settingValue = testFixture.Configuration.GetSection(configurationTypeProperty.Key).Get<string>();
                settingValue.ShouldBe(configurationTypeProperty.Value);
            }
        }
    }
}