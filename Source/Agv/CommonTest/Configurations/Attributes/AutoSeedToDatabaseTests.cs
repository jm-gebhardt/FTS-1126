using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Gebhardt.StoreWare.Agv.Common.Configurations;

namespace Gebhardt.StoreWare.Agv.CommonTest.Configurations.Attributes;

[TestClass]
public class AutoSeedToDatabaseTests
{
    [DataTestMethod]
    [DataRow("TestSettingWithAutoSeedAttributeFalse", false)]
    [DataRow("TestSettingWithAutoSeedAttributeTrue", true)]
    [DataRow("TestSettingWithoutAutoSeedAttribute", true)]
    public void InitializeConfigurationTypes_WhereAttributePropertyIsAppliedToClass_SetsCorrectAutoSeedToDatabaseInConfigurationType(string configurationTypeName, bool expectedAutoSeedToDatabase)
    {
        var typeManager = new ConfigurationTypeManager();

        var configurationType = typeManager.ConfigurationTypes.First(config => config.Name == configurationTypeName);
        configurationType.Name.ShouldBe(configurationTypeName);
        configurationType.AutoSeedToDatabase.ShouldBe(expectedAutoSeedToDatabase);
    }

    [DataTestMethod]
    [DataRow("TestSettingWithAutoSeedAttributeFalseForProperty", false)]
    [DataRow("TestSettingWithAutoSeedAttributeTrueForProperty", true)]
    public void InitializeConfigurationTypes_WhereAttributePropertyIsAppliedToProperty_SetsCorrectAutoSeedToDatabaseInConfigurationType(string configurationTypeName, bool expectedAutoSeedToDatabase)
    {
        var typeManager = new ConfigurationTypeManager();

        var configurationType = typeManager.ConfigurationTypes.First(config => config.Name == configurationTypeName);
        configurationType.Name.ShouldBe(configurationTypeName);
        foreach (var property in configurationType.Properties.Where(p => !p.Key.Contains($":{nameof(ISettings<AutoSeedToDatabaseTests>.SectionName)}")))
        {
            property.AutoSeedToDatabase.ShouldBe(expectedAutoSeedToDatabase);
        }
    }
}