using Gebhardt.StoreWare.Agv.Common.Configurations.Attributes;
using Gebhardt.StoreWare.Agv.Common.Configurations.Models;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.CommonTest.Configurations.Attributes;

[AutoSeedToDatabase(false)]
[UsedImplicitly]
public record TestSettingWithAutoSeedAttributeFalse : SettingsBase<TestSettingWithAutoSeedAttributeFalse>;

[AutoSeedToDatabase(true)]
[UsedImplicitly]
public record TestSettingWithAutoSeedAttributeTrue : SettingsBase<TestSettingWithAutoSeedAttributeTrue>;

[UsedImplicitly]
public record TestSettingWithoutAutoSeedAttribute : SettingsBase<TestSettingWithoutAutoSeedAttribute>;

[UsedImplicitly]
public record TestSettingWithAutoSeedAttributeFalseForProperty : SettingsBase<TestSettingWithAutoSeedAttributeFalseForProperty>
{
    [AutoSeedToDatabase(false)]
    [UsedImplicitly]
    public int TestPropertyFalse { get; init; } = 1;
}

[UsedImplicitly]
public record TestSettingWithAutoSeedAttributeTrueForProperty : SettingsBase<TestSettingWithAutoSeedAttributeTrueForProperty>
{
    [AutoSeedToDatabase(true)]
    [UsedImplicitly]
    public int TestPropertyTrue { get; init; } = 2;
}