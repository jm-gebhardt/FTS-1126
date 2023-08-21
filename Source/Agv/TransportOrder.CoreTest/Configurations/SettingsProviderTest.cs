using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using static Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Configurations.ConfigurationProviderTestFixture;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Configurations;

[TestClass]
public class SettingsProviderTest
{
        
    [TestMethod]
    public void GetSettings_WhenUsingDefaultConfigurationProvider_ReturnDefaultSettings()
    {
        var testFixture = new ConfigurationProviderTestFixture()
            .AddSettingsProvider<TestSettings>()
            .AddDefaultConfigurationProvider()
            .Build();

        ISettingsProvider<TestSettings> settingsProvider = testFixture.GetSettingsProvider<TestSettings>();

        var expectedSettings = new TestSettings();
        var actualSettings = settingsProvider.GetSettings();

        actualSettings.ShouldNotBeNull();
        actualSettings.BoolValue.ShouldBe(expectedSettings.BoolValue);
        actualSettings.IntValue.ShouldBe(expectedSettings.IntValue);
        TestSettings.EnumValue.ShouldBe(TestSettings.EnumValue);
    }
        
    [TestMethod]
    public void GetSettings_DefaultAndEmptyDatabaseRegistered_ReturnDefaultSettings()
    {
        var testFixture = new ConfigurationProviderTestFixture()
            .AddSettingsProvider<TestSettings>()
            .AddDefaultConfigurationProvider()
            .AddDatabase()
            .AddDatabaseConfigurationProvider()
            .Build();

        ISettingsProvider<TestSettings> settingsProvider = testFixture.GetSettingsProvider<TestSettings>();

        var expectedSettings = new TestSettings();
        var actualSettings = settingsProvider.GetSettings();

        actualSettings.ShouldNotBeNull();
        actualSettings.BoolValue.ShouldBe(expectedSettings.BoolValue);
        actualSettings.IntValue.ShouldBe(expectedSettings.IntValue);
        TestSettings.EnumValue.ShouldBe(TestSettings.EnumValue);
    }
        
    [TestMethod]
    public async Task GetSettings_DefaultAndDatabaseRegistered_DatabaseSettingOverrides()
    {
        var testFixture = new ConfigurationProviderTestFixture()
            .AddSettingsProvider<TestSettings>()
            .AddDefaultConfigurationProvider()
            .AddDatabase()
            .AddDatabaseConfigurationProvider()
            .Build();

        ISettingsProvider<TestSettings> settingsProvider = testFixture.GetSettingsProvider<TestSettings>();

        var expectedSettings = new TestSettings();
        var actualSettings = settingsProvider.GetSettings();

        actualSettings.ShouldNotBeNull();
        actualSettings.BoolValue.ShouldBe(expectedSettings.BoolValue);
        actualSettings.IntValue.ShouldBe(expectedSettings.IntValue);
        TestSettings.EnumValue.ShouldBe(TestSettings.EnumValue);

        await testFixture.UpdateConfigurationSettingAsync($"{nameof(TestSettings)}:{nameof(TestSettings.BoolValue)}", false.ToString(), nameof(TestSettings));
       
        var updatedSettings = settingsProvider.GetSettings();

        updatedSettings.ShouldNotBeNull();
        updatedSettings.BoolValue.ShouldBe(false);
        updatedSettings.IntValue.ShouldBe(expectedSettings.IntValue);
        TestSettings.EnumValue.ShouldBe(TestSettings.EnumValue);
    }
}