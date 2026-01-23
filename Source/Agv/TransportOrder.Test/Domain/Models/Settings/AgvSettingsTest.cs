using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models.Settings;

[TestClass]
public class AgvSettingsTest
{
    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        var groupId = Guid.NewGuid();
        var agvSettings = new AgvSettings(groupId);

        agvSettings.Id.ShouldNotBe(Guid.Empty);
        agvSettings.AgvSettingsGroupId.ShouldBe(groupId);
        agvSettings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(null!);
        agvSettings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(null!);
        agvSettings.MinChargeUp.ShouldBe(null!);
        agvSettings.MaxChargeUp.ShouldBe(null!);
        agvSettings.BatteryLevelCritical.ShouldBe(null!);
        agvSettings.BatteryLevelLow.ShouldBe(null!);
    }

    [TestMethod]
    public void UpdateBatteryLevelCritical_New_BatteryLevelCriticalUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.BatteryLevelCritical.ShouldBe(null!);
        var settings = new AgvSettingsReadModel(batteryLevelCritical:12);
        const bool isDefault = false;

        agvSettings.Update(settings, isDefault);
        agvSettings.BatteryLevelCritical.ShouldNotBe(null!);
        agvSettings.BatteryLevelCriticalId.ShouldBe(agvSettings.BatteryLevelCriticalId);
        agvSettings.BatteryLevelCritical.Value.ShouldBe(settings.BatteryLevelCritical.ToString());
        agvSettings.BatteryLevelCritical.Name.ShouldBe(nameof(settings.BatteryLevelCritical));
        agvSettings.BatteryLevelCritical.ValueType.ShouldBe(settings.BatteryLevelCritical.GetType().FullName);
        agvSettings.BatteryLevelCritical.IsDefault.ShouldBe(isDefault);
    }

    [TestMethod]
    public void UpdateBatteryLevelCritical_ExistingNonDefault_BatteryLevelCriticalUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, false);
        agvSettings.BatteryLevelCritical.ShouldNotBe(null!);
        var id = agvSettings.BatteryLevelCritical.Id;

        var newSettings = new AgvSettingsReadModel(batteryLevelCritical: 20);
        agvSettings.Update(newSettings, false);
        agvSettings.BatteryLevelCritical.Value.ShouldBe(newSettings.BatteryLevelCritical.ToString());
        agvSettings.BatteryLevelCriticalId.ShouldBe(id);
        agvSettings.BatteryLevelCriticalId.ShouldBe(agvSettings.BatteryLevelCriticalId);
        agvSettings.BatteryLevelCritical.IsDefault.ShouldBe(false);
    }

    [TestMethod]
    public void UpdateBatteryLevelCritical_ExistingDefault_BatteryLevelCriticalUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, true);
        agvSettings.BatteryLevelCritical.ShouldNotBe(null!);
        var defaultId = agvSettings.BatteryLevelCritical.Id;

        var newSettings = new AgvSettingsReadModel(batteryLevelCritical: 27);
        agvSettings.Update(newSettings, false);
        agvSettings.BatteryLevelCritical.Value.ShouldBe(newSettings.BatteryLevelCritical.ToString());
        agvSettings.BatteryLevelCriticalId.ShouldNotBe(defaultId);
        agvSettings.BatteryLevelCriticalId.ShouldBe(agvSettings.BatteryLevelCriticalId);
        agvSettings.BatteryLevelCritical.IsDefault.ShouldBe(false);
    }

    [TestMethod]
    public void UpdateBatteryLevelCritical_InvalidData_BatteryLevelCriticalNotUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, true);
        agvSettings.BatteryLevelCritical.ShouldNotBe(null!);
        var defaultId = agvSettings.BatteryLevelCritical.Id;

        var newSettings = new AgvSettingsReadModel(batteryLevelCritical: -1);
        agvSettings.Update(newSettings, false);
        agvSettings.BatteryLevelCritical.Value.ShouldBe(EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical.ToString());
        agvSettings.BatteryLevelCriticalId.ShouldBe(defaultId);
        agvSettings.BatteryLevelCriticalId.ShouldBe(agvSettings.BatteryLevelCriticalId);
        agvSettings.BatteryLevelCritical.IsDefault.ShouldBe(true);
    }

    [TestMethod]
    public void UpdateAgvOfflineTimeoutMsWhileIdle_New_AgvOfflineTimeoutMsWhileIdleUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(null!);
        var settings = new AgvSettingsReadModel(agvOfflineTimeoutMsWhileIdle:2000) ;
        const bool isDefault = false;

        agvSettings.Update(settings, isDefault);
        agvSettings.AgvOfflineTimeoutMsWhileIdle.ShouldNotBe(null!);
        agvSettings.AgvOfflineTimeoutMsWhileIdleId.ShouldBe(agvSettings.AgvOfflineTimeoutMsWhileIdleId);
        agvSettings.AgvOfflineTimeoutMsWhileIdle.Value.ShouldBe(settings.AgvOfflineTimeoutMsWhileIdle.ToString());
        agvSettings.AgvOfflineTimeoutMsWhileIdle.Name.ShouldBe(nameof(settings.AgvOfflineTimeoutMsWhileIdle));
        agvSettings.AgvOfflineTimeoutMsWhileIdle.ValueType.ShouldBe(settings.AgvOfflineTimeoutMsWhileIdle.GetType().FullName);
        agvSettings.AgvOfflineTimeoutMsWhileIdle.IsDefault.ShouldBe(isDefault);
    }

    [TestMethod]
    public void UpdateAgvOfflineTimeoutMsWhileIdle_ExistingNonDefault_AgvOfflineTimeoutMsWhileIdleUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, false);
        agvSettings.AgvOfflineTimeoutMsWhileIdle.ShouldNotBe(null!);
        var id = agvSettings.AgvOfflineTimeoutMsWhileIdle.Id;

        var newSettings = new AgvSettingsReadModel(agvOfflineTimeoutMsWhileIdle: 1000);
        agvSettings.Update(newSettings, false);
        agvSettings.AgvOfflineTimeoutMsWhileIdle.Value.ShouldBe(newSettings.AgvOfflineTimeoutMsWhileIdle.ToString());
        agvSettings.AgvOfflineTimeoutMsWhileIdleId.ShouldBe(id);
        agvSettings.AgvOfflineTimeoutMsWhileIdleId.ShouldBe(agvSettings.AgvOfflineTimeoutMsWhileIdleId);
        agvSettings.AgvOfflineTimeoutMsWhileIdle.IsDefault.ShouldBe(false);
    }

    [TestMethod]
    public void UpdateAgvOfflineTimeoutMsWhileIdle_ExistingDefault_AgvOfflineTimeoutMsWhileIdleUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, true);
        agvSettings.AgvOfflineTimeoutMsWhileIdle.ShouldNotBe(null!);
        var defaultId = agvSettings.AgvOfflineTimeoutMsWhileIdle.Id;

        var newSettings = new AgvSettingsReadModel(agvOfflineTimeoutMsWhileIdle: 1000);
        agvSettings.Update(newSettings, false);
        agvSettings.AgvOfflineTimeoutMsWhileIdle.Value.ShouldBe(newSettings.AgvOfflineTimeoutMsWhileIdle.ToString());
        agvSettings.AgvOfflineTimeoutMsWhileIdleId.ShouldNotBe(defaultId);
        agvSettings.AgvOfflineTimeoutMsWhileIdleId.ShouldBe(agvSettings.AgvOfflineTimeoutMsWhileIdleId);
        agvSettings.AgvOfflineTimeoutMsWhileIdle.IsDefault.ShouldBe(false);
    }

    [TestMethod]
    public void UpdateAgvOfflineTimeoutMsWhileIdle_InvalidData_AgvOfflineTimeoutMsWhileIdleNotUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, true);
        agvSettings.AgvOfflineTimeoutMsWhileIdle.ShouldNotBe(null!);
        var defaultId = agvSettings.AgvOfflineTimeoutMsWhileIdle.Id;

        var newSettings = new AgvSettingsReadModel(agvOfflineTimeoutMsWhileIdle: -1);
        agvSettings.Update(newSettings, false);
        agvSettings.AgvOfflineTimeoutMsWhileIdle.Value.ShouldBe(EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle.ToString());
        agvSettings.AgvOfflineTimeoutMsWhileIdleId.ShouldBe(defaultId);
        agvSettings.AgvOfflineTimeoutMsWhileIdleId.ShouldBe(agvSettings.AgvOfflineTimeoutMsWhileIdleId);
        agvSettings.AgvOfflineTimeoutMsWhileIdle.IsDefault.ShouldBe(true);
    }

    [TestMethod]
    public void UpdateAgvOfflineTimeoutMsWhileBusy_New_AgvOfflineTimeoutMsWhileBusyUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(null!);
        var settings = new AgvSettingsReadModel(agvOfflineTimeoutMsWhileBusy: 1000);
        const bool isDefault = false;

        agvSettings.Update(settings, isDefault);
        agvSettings.AgvOfflineTimeoutMsWhileBusy.ShouldNotBe(null!);
        agvSettings.AgvOfflineTimeoutMsWhileBusyId.ShouldBe(agvSettings.AgvOfflineTimeoutMsWhileBusyId);
        agvSettings.AgvOfflineTimeoutMsWhileBusy.Value.ShouldBe(settings.AgvOfflineTimeoutMsWhileBusy.ToString());
        agvSettings.AgvOfflineTimeoutMsWhileBusy.Name.ShouldBe(nameof(settings.AgvOfflineTimeoutMsWhileBusy));
        agvSettings.AgvOfflineTimeoutMsWhileBusy.ValueType.ShouldBe(settings.AgvOfflineTimeoutMsWhileBusy.GetType().FullName);
        agvSettings.AgvOfflineTimeoutMsWhileBusy.IsDefault.ShouldBe(isDefault);
    }

    [TestMethod]
    public void UpdateAgvOfflineTimeoutMsWhileBusy_ExistingNonDefault_AgvOfflineTimeoutMsWhileBusyUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, false);
        agvSettings.AgvOfflineTimeoutMsWhileBusy.ShouldNotBe(null!);
        var id = agvSettings.AgvOfflineTimeoutMsWhileBusy.Id;

        var newSettings = new AgvSettingsReadModel(agvOfflineTimeoutMsWhileBusy: 2000);
        agvSettings.Update(newSettings, false);
        agvSettings.AgvOfflineTimeoutMsWhileBusy.Value.ShouldBe(newSettings.AgvOfflineTimeoutMsWhileBusy.ToString());
        agvSettings.AgvOfflineTimeoutMsWhileBusyId.ShouldBe(id);
        agvSettings.AgvOfflineTimeoutMsWhileBusyId.ShouldBe(agvSettings.AgvOfflineTimeoutMsWhileBusyId);
        agvSettings.AgvOfflineTimeoutMsWhileBusy.IsDefault.ShouldBe(false);
    }

    [TestMethod]
    public void UpdateAgvOfflineTimeoutMsWhileBusy_ExistingDefault_AgvOfflineTimeoutMsWhileBusyUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, true);
        agvSettings.AgvOfflineTimeoutMsWhileBusy.ShouldNotBe(null!);
        var defaultId = agvSettings.AgvOfflineTimeoutMsWhileBusy.Id;

        var newSettings = new AgvSettingsReadModel(agvOfflineTimeoutMsWhileBusy: 2000);
        agvSettings.Update(newSettings, false);
        agvSettings.AgvOfflineTimeoutMsWhileBusy.Value.ShouldBe(newSettings.AgvOfflineTimeoutMsWhileBusy.ToString());
        agvSettings.AgvOfflineTimeoutMsWhileBusyId.ShouldNotBe(defaultId);
        agvSettings.AgvOfflineTimeoutMsWhileBusyId.ShouldBe(agvSettings.AgvOfflineTimeoutMsWhileBusyId);
        agvSettings.AgvOfflineTimeoutMsWhileBusy.IsDefault.ShouldBe(false);
    }

    [TestMethod]
    public void UpdateAgvOfflineTimeoutMsWhileBusy_InvalidData_AgvOfflineTimeoutMsWhileBusyNotUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, true);
        agvSettings.AgvOfflineTimeoutMsWhileBusy.ShouldNotBe(null!);
        var defaultId = agvSettings.AgvOfflineTimeoutMsWhileBusy.Id;

        var newSettings = new AgvSettingsReadModel(agvOfflineTimeoutMsWhileBusy: -1);
        agvSettings.Update(newSettings, false);
        agvSettings.AgvOfflineTimeoutMsWhileBusy.Value.ShouldBe(EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy.ToString());
        agvSettings.AgvOfflineTimeoutMsWhileBusyId.ShouldBe(defaultId);
        agvSettings.AgvOfflineTimeoutMsWhileBusyId.ShouldBe(agvSettings.AgvOfflineTimeoutMsWhileBusyId);
        agvSettings.AgvOfflineTimeoutMsWhileBusy.IsDefault.ShouldBe(true);
    }

    [TestMethod]
    public void UpdateBatteryLevelLow_New_BatteryLevelLowUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.BatteryLevelLow.ShouldBe(null!);
        var settings = new AgvSettingsReadModel(batteryLevelLow: 10);
        const bool isDefault = false;

        agvSettings.Update(settings, isDefault);
        agvSettings.BatteryLevelLow.ShouldNotBe(null!);
        agvSettings.BatteryLevelLowId.ShouldBe(agvSettings.BatteryLevelLowId);
        agvSettings.BatteryLevelLow.Value.ShouldBe(settings.BatteryLevelLow.ToString());
        agvSettings.BatteryLevelLow.Name.ShouldBe(nameof(settings.BatteryLevelLow));
        agvSettings.BatteryLevelLow.ValueType.ShouldBe(settings.BatteryLevelLow.GetType().FullName);
        agvSettings.BatteryLevelLow.IsDefault.ShouldBe(isDefault);
    }

    [TestMethod]
    public void UpdateBatteryLevelLow_ExistingNonDefault_BatteryLevelLowUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, false);
        agvSettings.BatteryLevelLow.ShouldNotBe(null!);
        var id = agvSettings.BatteryLevelLow.Id;

        var newSettings = new AgvSettingsReadModel(batteryLevelLow: 30);
        agvSettings.Update(newSettings, false);
        agvSettings.BatteryLevelLow.Value.ShouldBe(newSettings.BatteryLevelLow.ToString());
        agvSettings.BatteryLevelLowId.ShouldBe(id);
        agvSettings.BatteryLevelLowId.ShouldBe(agvSettings.BatteryLevelLowId);
        agvSettings.BatteryLevelLow.IsDefault.ShouldBe(false);
    }

    [TestMethod]
    public void UpdateBatteryLevelLow_ExistingDefault_BatteryLevelLowUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, true);
        agvSettings.BatteryLevelLow.ShouldNotBe(null!);
        var defaultId = agvSettings.BatteryLevelLow.Id;

        var newSettings = new AgvSettingsReadModel(batteryLevelLow: 30);
        agvSettings.Update(newSettings, false);
        agvSettings.BatteryLevelLow.Value.ShouldBe(newSettings.BatteryLevelLow.ToString());
        agvSettings.BatteryLevelLowId.ShouldNotBe(defaultId);
        agvSettings.BatteryLevelLowId.ShouldBe(agvSettings.BatteryLevelLowId);
        agvSettings.BatteryLevelLow.IsDefault.ShouldBe(false);
    }

    [TestMethod]
    public void UpdateBatteryLevelLow_InvalidData_BatteryLevelLowNotUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, true);
        agvSettings.BatteryLevelLow.ShouldNotBe(null!);
        var defaultId = agvSettings.BatteryLevelLow.Id;

        var newSettings = new AgvSettingsReadModel(batteryLevelLow: -1);
        agvSettings.Update(newSettings, false);
        agvSettings.BatteryLevelLow.Value.ShouldBe(EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow.ToString());
        agvSettings.BatteryLevelLowId.ShouldBe(defaultId);
        agvSettings.BatteryLevelLowId.ShouldBe(agvSettings.BatteryLevelLowId);
        agvSettings.BatteryLevelLow.IsDefault.ShouldBe(true);
    }

    [TestMethod]
    public void UpdateMinChargeUp_New_MinChargeUpUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.MinChargeUp.ShouldBe(null!);
        var settings = new AgvSettingsReadModel(minChargeUp: 20);
        const bool isDefault = false;

        agvSettings.Update(settings, isDefault);
        agvSettings.MinChargeUp.ShouldNotBe(null!);
        agvSettings.MinChargeUpId.ShouldBe(agvSettings.MinChargeUpId);
        agvSettings.MinChargeUp.Value.ShouldBe(settings.MinChargeUp.ToString());
        agvSettings.MinChargeUp.Name.ShouldBe(nameof(settings.MinChargeUp));
        agvSettings.MinChargeUp.ValueType.ShouldBe(settings.MinChargeUp.GetType().FullName);
        agvSettings.MinChargeUp.IsDefault.ShouldBe(isDefault);
    }

    [TestMethod]
    public void UpdateMinChargeUp_ExistingNonDefault_MinChargeUpUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, false);
        agvSettings.MinChargeUp.ShouldNotBe(null!);
        var id = agvSettings.MinChargeUp.Id;

        var newSettings = new AgvSettingsReadModel(minChargeUp: 20);
        agvSettings.Update(newSettings, false);
        agvSettings.MinChargeUp.Value.ShouldBe(newSettings.MinChargeUp.ToString());
        agvSettings.MinChargeUpId.ShouldBe(id);
        agvSettings.MinChargeUpId.ShouldBe(agvSettings.MinChargeUpId);
        agvSettings.MinChargeUp.IsDefault.ShouldBe(false);
    }

    [TestMethod]
    public void UpdateMinChargeUp_ExistingDefault_MinChargeUpUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, true);
        agvSettings.MinChargeUp.ShouldNotBe(null!);
        var defaultId = agvSettings.MinChargeUp.Id;

        var newSettings = new AgvSettingsReadModel(minChargeUp: 15);
        agvSettings.Update(newSettings, false);
        agvSettings.MinChargeUp.Value.ShouldBe(newSettings.MinChargeUp.ToString());
        agvSettings.MinChargeUpId.ShouldNotBe(defaultId);
        agvSettings.MinChargeUpId.ShouldBe(agvSettings.MinChargeUpId);
        agvSettings.MinChargeUp.IsDefault.ShouldBe(false);
    }

    [TestMethod]
    public void UpdateMinChargeUp_InvalidData_MinChargeUpNotUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, true);
        agvSettings.MinChargeUp.ShouldNotBe(null!);
        var defaultId = agvSettings.MinChargeUp.Id;

        var newSettings = new AgvSettingsReadModel(minChargeUp: -1);
        agvSettings.Update(newSettings, false);
        agvSettings.MinChargeUp.Value.ShouldBe(EfAgvSettingsProvider.DefaultSettings.MinChargeUp.ToString());
        agvSettings.MinChargeUpId.ShouldBe(defaultId);
        agvSettings.MinChargeUpId.ShouldBe(agvSettings.MinChargeUpId);
        agvSettings.MinChargeUp.IsDefault.ShouldBe(true);
    }

    [TestMethod]
    public void UpdateMaxChargeUp_New_MaxChargeUpUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.MaxChargeUp.ShouldBe(null!);
        var settings = new AgvSettingsReadModel(maxChargeUp: 85);
        const bool isDefault = false;

        agvSettings.Update(settings, isDefault);
        agvSettings.MaxChargeUp.ShouldNotBe(null!);
        agvSettings.MaxChargeUpId.ShouldBe(agvSettings.MaxChargeUpId);
        agvSettings.MaxChargeUp.Value.ShouldBe(settings.MaxChargeUp.ToString());
        agvSettings.MaxChargeUp.Name.ShouldBe(nameof(settings.MaxChargeUp));
        agvSettings.MaxChargeUp.ValueType.ShouldBe(settings.MaxChargeUp.GetType().FullName);
        agvSettings.MaxChargeUp.IsDefault.ShouldBe(isDefault);
    }

    [TestMethod]
    public void UpdateMaxChargeUp_ExistingNonDefault_MaxChargeUpUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, false);
        agvSettings.MaxChargeUp.ShouldNotBe(null!);
        var id = agvSettings.MaxChargeUp.Id;

        var newSettings = new AgvSettingsReadModel(maxChargeUp: 85);
        agvSettings.Update(newSettings, false);
        agvSettings.MaxChargeUp.Value.ShouldBe(newSettings.MaxChargeUp.ToString());
        agvSettings.MaxChargeUpId.ShouldBe(id);
        agvSettings.MaxChargeUpId.ShouldBe(agvSettings.MaxChargeUpId);
        agvSettings.MaxChargeUp.IsDefault.ShouldBe(false);
    }

    [TestMethod]
    public void UpdateMaxChargeUp_ExistingDefault_MaxChargeUpUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, true);
        agvSettings.MaxChargeUp.ShouldNotBe(null!);
        var defaultId = agvSettings.MaxChargeUp.Id;

        var newSettings = new AgvSettingsReadModel(maxChargeUp: 85);
        agvSettings.Update(newSettings, false);
        agvSettings.MaxChargeUp.Value.ShouldBe(newSettings.MaxChargeUp.ToString());
        agvSettings.MaxChargeUpId.ShouldNotBe(defaultId);
        agvSettings.MaxChargeUpId.ShouldBe(agvSettings.MaxChargeUpId);
        agvSettings.MaxChargeUp.IsDefault.ShouldBe(false);
    }

    [TestMethod]
    public void UpdateMaxChargeUp_InvalidData_MaxChargeUpNotUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, true);
        agvSettings.MaxChargeUp.ShouldNotBe(null!);
        var defaultId = agvSettings.MaxChargeUp.Id;

        var newSettings = new AgvSettingsReadModel(maxChargeUp: -1);
        agvSettings.Update(newSettings, false);
        agvSettings.MaxChargeUp.Value.ShouldBe(EfAgvSettingsProvider.DefaultSettings.MaxChargeUp.ToString());
        agvSettings.MaxChargeUpId.ShouldBe(defaultId);
        agvSettings.MaxChargeUpId.ShouldBe(agvSettings.MaxChargeUpId);
        agvSettings.MaxChargeUp.IsDefault.ShouldBe(true);
    }

    [TestMethod]
    public void Update_ValidData_AllPropertiesUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        var settings = EfAgvSettingsProvider.DefaultSettings;
        const bool isDefault = false;
        agvSettings.Update(settings, isDefault);

        agvSettings.MaxChargeUp.IsDefault.ShouldBe(isDefault);
        agvSettings.MaxChargeUp.Value.ShouldBe(settings.MaxChargeUp.ToString());
        agvSettings.MinChargeUp.IsDefault.ShouldBe(isDefault);
        agvSettings.MinChargeUp.Value.ShouldBe(settings.MinChargeUp.ToString());
        agvSettings.BatteryLevelCritical.IsDefault.ShouldBe(isDefault);
        agvSettings.BatteryLevelCritical.Value.ShouldBe(settings.BatteryLevelCritical.ToString());
        agvSettings.BatteryLevelLow.IsDefault.ShouldBe(isDefault);
        agvSettings.BatteryLevelLow.Value.ShouldBe(settings.BatteryLevelLow.ToString());
        agvSettings.AgvOfflineTimeoutMsWhileIdle.IsDefault.ShouldBe(isDefault);
        agvSettings.AgvOfflineTimeoutMsWhileIdle.Value.ShouldBe(settings.AgvOfflineTimeoutMsWhileIdle.ToString());
        agvSettings.AgvOfflineTimeoutMsWhileBusy.IsDefault.ShouldBe(isDefault);
        agvSettings.AgvOfflineTimeoutMsWhileBusy.Value.ShouldBe(settings.AgvOfflineTimeoutMsWhileBusy.ToString());
    }

    [TestMethod]
    public void GetSettingByName_ValidData_ReturnsSetting()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, false);

        agvSettings.GetSettingByName(agvSettings.BatteryLevelCritical.Name).ShouldBe(agvSettings.BatteryLevelCritical);
    }

    [TestMethod]
    public void GetSettingByName_UnknownSettingName_ThrowsInvalidAgvSettingsPropertyException()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, false);
        
       Should.Throw<InvalidAgvSettingsPropertyException>(() => agvSettings.GetSettingByName("unknown"));
    }

    [TestMethod]
    public void UpdatePropertyId_ValidData_IdPropertyUpdated()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, false);
        var newId = Guid.NewGuid();
        
        agvSettings.UpdatePropertyId(nameof(agvSettings.AgvOfflineTimeoutMsWhileIdle), newId);
        agvSettings.AgvOfflineTimeoutMsWhileIdleId.ShouldBe(newId);
    }

    [TestMethod]
    public void ReplaceSetting_ValidData_SettingReplaced()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, false);

        var replacement = new AgvSettings(Guid.NewGuid());
        replacement.Update(EfAgvSettingsProvider.DefaultSettings, false);

        agvSettings.BatteryLevelLow.ShouldNotBe(replacement.BatteryLevelLow);
        agvSettings.BatteryLevelLowId.ShouldNotBe(replacement.BatteryLevelLowId);

        agvSettings.ReplaceSetting(replacement.BatteryLevelLow);

        agvSettings.BatteryLevelLowId.ShouldBe(replacement.BatteryLevelLowId);
        agvSettings.BatteryLevelLow.ShouldBe(replacement.BatteryLevelLow);
    }

    [TestMethod]
    public void ReplaceSetting_UnknownSetting_ThrowsInvalidAgvSettingsPropertyException()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, false);
        
        Should.Throw<InvalidAgvSettingsPropertyException>(() =>
            agvSettings.ReplaceSetting(new Setting(Guid.NewGuid(), "unknown", "value", "type", false)));
    }

    [TestMethod]
    public void GetAllSettings_ValidData_SettingsReturned()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, false);

        ICollection<Setting?> settings = agvSettings.GetAllSettings();

        foreach ((string name, object? value) in EfAgvSettingsProvider.DefaultSettings.GetValidPropertiesInformation())
        {
            settings.Any(s => string.Equals(s!.Name, name)).ShouldBeTrue();
            settings.Any(s => string.Equals(s!.Value, value!.ToString()!, StringComparison.InvariantCultureIgnoreCase)).ShouldBeTrue();
        }
    }

    [TestMethod]
    public void ReplaceAllSettings_ValidData_ReplacesSettings()
    {
        var agvSettings = new AgvSettings(Guid.NewGuid());
        agvSettings.Update(EfAgvSettingsProvider.DefaultSettings, false);

        var newSettings = new AgvSettings(Guid.NewGuid());
        newSettings.Update(EfAgvSettingsProvider.DefaultSettings, false);
        agvSettings.GetAllSettings().ShouldNotBeSameAs(newSettings.GetAllSettings());

        agvSettings.ReplaceAllSettings(newSettings);
        agvSettings.GetAllSettings().ShouldBe(newSettings.GetAllSettings(), ignoreOrder:true);
    }
}