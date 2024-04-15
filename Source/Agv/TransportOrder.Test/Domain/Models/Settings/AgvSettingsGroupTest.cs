using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Domain.Models.Settings;

[TestClass]
public class AgvSettingsGroupTest
{
    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        const GroupType groupType = GroupType.SerialNumber;
        const string identifier = "an agv serial number";
        var settingsGroup = new AgvSettingsGroup(groupType, identifier);

        settingsGroup.GroupType.ShouldBe(groupType);
        settingsGroup.Identifier.ShouldBe(identifier);
        settingsGroup.Id.ShouldNotBe(Guid.Empty);
        settingsGroup.AgvSetting.ShouldNotBe(null!);
        settingsGroup.AgvSetting.AgvGroupId.ShouldBe(settingsGroup.Id);
        settingsGroup.AgvSetting.BatteryLevelCritical.ShouldBe(null!);
        settingsGroup.AgvSetting.BatteryLevelLow.ShouldBe(null!);
        settingsGroup.AgvSetting.MaxChargeUp.ShouldBe(null!);
        settingsGroup.AgvSetting.MinChargeUp.ShouldBe(null!);
        settingsGroup.AgvSetting.AgvOfflineTimeoutMsWhileBusy.ShouldBe(null!);
        settingsGroup.AgvSetting.AgvOfflineTimeoutMsWhileIdle.ShouldBe(null!);
    }

    [TestMethod]
    public void UpdateSettings_GroupTypeAll_IsDefault()
    {
        var settingsGroup = new AgvSettingsGroup(GroupType.All);
        settingsGroup.UpdateSettings(EfAgvSettingsProvider.DefaultSettings);
        settingsGroup.AgvSetting.BatteryLevelCritical.IsDefault.ShouldBeTrue();
        settingsGroup.AgvSetting.BatteryLevelLow.IsDefault.ShouldBeTrue();
        settingsGroup.AgvSetting.MaxChargeUp.IsDefault.ShouldBeTrue();
        settingsGroup.AgvSetting.MinChargeUp.IsDefault.ShouldBeTrue();
        settingsGroup.AgvSetting.AgvOfflineTimeoutMsWhileBusy.IsDefault.ShouldBeTrue();
        settingsGroup.AgvSetting.AgvOfflineTimeoutMsWhileIdle.IsDefault.ShouldBeTrue();
    }

    [TestMethod]
    public void UpdateSettings_GroupTypeNotAll_NotDefault()
    {
        var settingsGroup = new AgvSettingsGroup(GroupType.SerialNumber, "a serial number");
        settingsGroup.UpdateSettings(EfAgvSettingsProvider.DefaultSettings);
        settingsGroup.AgvSetting.BatteryLevelCritical.IsDefault.ShouldBeFalse();
        settingsGroup.AgvSetting.BatteryLevelLow.IsDefault.ShouldBeFalse();
        settingsGroup.AgvSetting.MaxChargeUp.IsDefault.ShouldBeFalse();
        settingsGroup.AgvSetting.MinChargeUp.IsDefault.ShouldBeFalse();
        settingsGroup.AgvSetting.AgvOfflineTimeoutMsWhileBusy.IsDefault.ShouldBeFalse();
        settingsGroup.AgvSetting.AgvOfflineTimeoutMsWhileIdle.IsDefault.ShouldBeFalse();
    }
}