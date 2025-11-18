using Gebhardt.Acc.Common.Configurations;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Configurations.Attributes;

[TestClass]
public class AutoSeedToDatabaseTests
{
    [TestMethod]
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

    [TestMethod]
    [DataRow("TestSettingWithAutoSeedAttributeFalseForProperty", false)]
    [DataRow("TestSettingWithAutoSeedAttributeTrueForProperty", true)]
    public void InitializeConfigurationTypes_WhereAttributePropertyIsAppliedToProperty_SetsCorrectAutoSeedToDatabaseInConfigurationType(string configurationTypeName, bool expectedAutoSeedToDatabase)
    {
        var typeManager = new ConfigurationTypeManager();

        var configurationType = typeManager.ConfigurationTypes.First(config => config.Name == configurationTypeName);
        configurationType.Name.ShouldBe(configurationTypeName);
        foreach (var property in configurationType.Properties.Where(p => !p.Key.Contains($":{nameof(ISettings<>.SectionName)}")))
        {
            property.AutoSeedToDatabase.ShouldBe(expectedAutoSeedToDatabase);
        }
    }
}