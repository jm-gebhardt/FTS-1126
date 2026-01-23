using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models.Settings;

[TestClass]
public class AgvSettingsReadModelTest
{
    [TestMethod]
    public void Construct_AllValuesSet_PropertiesSet()
    {
        var settings = new AgvSettingsReadModel(
            EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle,
            EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy,
            EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical,
            EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow,
            EfAgvSettingsProvider.DefaultSettings.MinChargeUp,
            EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);

        settings.ShouldBeEquivalentTo(EfAgvSettingsProvider.DefaultSettings);
    }

    [TestMethod]
    public void Construct_AgvOfflineTimeoutMsWhileIdleNotSet_AgvOfflineTimeoutMsWhileIdleInitializedWithNegativeValue()
    {
        var settings = new AgvSettingsReadModel(
            agvOfflineTimeoutMsWhileBusy:EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy,
            batteryLevelCritical: EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical,
            batteryLevelLow : EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow,
            minChargeUp: EfAgvSettingsProvider.DefaultSettings.MinChargeUp,
            maxChargeUp: EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);
        
        settings.AgvOfflineTimeoutMsWhileIdle.ShouldBeLessThan(0);
        settings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy);
        settings.BatteryLevelCritical.ShouldBe(EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical);
        settings.BatteryLevelLow.ShouldBe(EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        settings.MaxChargeUp.ShouldBe(EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);
        settings.MinChargeUp.ShouldBe(EfAgvSettingsProvider.DefaultSettings.MinChargeUp);
    }

    [TestMethod]
    public void Construct_AgvOfflineTimeoutMsWhileBusyNotSet_AgvOfflineTimeoutMsWhileBusyInitializedWithNegativeValue()
    {
        var settings = new AgvSettingsReadModel(
            agvOfflineTimeoutMsWhileIdle: EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle,
            batteryLevelCritical: EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical,
            batteryLevelLow: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow,
            minChargeUp: EfAgvSettingsProvider.DefaultSettings.MinChargeUp,
            maxChargeUp: EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);

        settings.AgvOfflineTimeoutMsWhileBusy.ShouldBeLessThan(0);
        settings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle);
        settings.BatteryLevelCritical.ShouldBe(EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical);
        settings.BatteryLevelLow.ShouldBe(EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        settings.MaxChargeUp.ShouldBe(EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);
        settings.MinChargeUp.ShouldBe(EfAgvSettingsProvider.DefaultSettings.MinChargeUp);
    }

    [TestMethod]
    public void Construct_BatteryLevelCriticalNotSet_BatteryLevelCriticalInitializedWithNegativeValue()
    {
        var settings = new AgvSettingsReadModel(
            agvOfflineTimeoutMsWhileIdle: EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle,
            agvOfflineTimeoutMsWhileBusy: EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy,
            batteryLevelLow: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow,
            minChargeUp: EfAgvSettingsProvider.DefaultSettings.MinChargeUp,
            maxChargeUp: EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);

        settings.BatteryLevelCritical.ShouldBeLessThan(0);
        settings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle);
        settings.BatteryLevelLow.ShouldBe(EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        settings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy);
        settings.MaxChargeUp.ShouldBe(EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);
        settings.MinChargeUp.ShouldBe(EfAgvSettingsProvider.DefaultSettings.MinChargeUp);
    }

    [TestMethod]
    public void Construct_BatteryLevelLowNotSet_BatteryLevelLowInitializedWithNegativeValue()
    {
        var settings = new AgvSettingsReadModel(
            agvOfflineTimeoutMsWhileIdle: EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle,
            agvOfflineTimeoutMsWhileBusy: EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy,
            batteryLevelCritical: EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical,
            minChargeUp: EfAgvSettingsProvider.DefaultSettings.MinChargeUp,
            maxChargeUp: EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);

        settings.BatteryLevelLow.ShouldBeLessThan(0);
        settings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle);
        settings.BatteryLevelCritical.ShouldBe(EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical);
        settings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy);
        settings.MaxChargeUp.ShouldBe(EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);
        settings.MinChargeUp.ShouldBe(EfAgvSettingsProvider.DefaultSettings.MinChargeUp);
    }

    [TestMethod]
    public void Construct_MaxChargeUpNotSet_MaxChargeUpInitializedWithNegativeValue()
    {
        var settings = new AgvSettingsReadModel(
            agvOfflineTimeoutMsWhileIdle: EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle,
            agvOfflineTimeoutMsWhileBusy: EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy,
            batteryLevelCritical: EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical,
            batteryLevelLow: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow,
            minChargeUp: EfAgvSettingsProvider.DefaultSettings.MinChargeUp);

        settings.MaxChargeUp.ShouldBeLessThan(0);
        settings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle);
        settings.BatteryLevelCritical.ShouldBe(EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical);
        settings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy);
        settings.BatteryLevelLow.ShouldBe(EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        settings.MinChargeUp.ShouldBe(EfAgvSettingsProvider.DefaultSettings.MinChargeUp);
    }

    [TestMethod]
    public void Construct_MinChargeUpNotSet_MinChargeUpInitializedWithNegativeValue()
    {
        var settings = new AgvSettingsReadModel(
            agvOfflineTimeoutMsWhileIdle: EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle,
            agvOfflineTimeoutMsWhileBusy: EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy,
            batteryLevelCritical: EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical,
            batteryLevelLow: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow,
            maxChargeUp: EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);

        settings.MinChargeUp.ShouldBeLessThan(0);
        settings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle);
        settings.BatteryLevelCritical.ShouldBe(EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical);
        settings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy);
        settings.BatteryLevelLow.ShouldBe(EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        settings.MaxChargeUp.ShouldBe(EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);
    }

    [TestMethod]
    public void Update_AllNewValuesNonNegative_AllValuesUpdated()
    {
        var settings = new AgvSettingsReadModel(
            agvOfflineTimeoutMsWhileBusy: EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy,
            batteryLevelCritical: EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical,
            batteryLevelLow: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow,
            minChargeUp: EfAgvSettingsProvider.DefaultSettings.MinChargeUp,
            maxChargeUp: EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);

        var newSettings = new AgvSettingsReadModel(
            agvOfflineTimeoutMsWhileIdle: 12,
            agvOfflineTimeoutMsWhileBusy: 12,
            batteryLevelCritical: 12,
            batteryLevelLow: 12,
            maxChargeUp: 12,
            minChargeUp: 12);

        settings.Update(newSettings);
        settings.ShouldBeEquivalentTo(newSettings);
    }

    [TestMethod]
    public void Update_NewValuesNegative_NegativeValuesNotUpdated()
    {
        var settings = new AgvSettingsReadModel(
            agvOfflineTimeoutMsWhileBusy: EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy,
            agvOfflineTimeoutMsWhileIdle: EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle,
            batteryLevelCritical: EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical,
            batteryLevelLow: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow,
            minChargeUp: EfAgvSettingsProvider.DefaultSettings.MinChargeUp,
            maxChargeUp: EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);

        var newSettings = new AgvSettingsReadModel(
            agvOfflineTimeoutMsWhileIdle: -12,
            maxChargeUp: -1,
            batteryLevelCritical: -14,
            agvOfflineTimeoutMsWhileBusy: -12,
            batteryLevelLow: -12,
            minChargeUp: -12);

        settings.Update(newSettings);

        settings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle);
        settings.MaxChargeUp.ShouldBe(EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);
        settings.BatteryLevelCritical.ShouldBe(EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical);
        settings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy);
        settings.BatteryLevelLow.ShouldBe(EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        settings.MinChargeUp.ShouldBe(EfAgvSettingsProvider.DefaultSettings.MinChargeUp);
    }

    [TestMethod]
    public void GetValidPropertiesInformation_ValidData_ReturnsValidPropertiesInformation()
    {
        const int validMaxChargeUp = 87; 
        var settings = new AgvSettingsReadModel(
            maxChargeUp: validMaxChargeUp,
            batteryLevelCritical: -1,
            agvOfflineTimeoutMsWhileBusy: -1,
            agvOfflineTimeoutMsWhileIdle: -1,
            batteryLevelLow: -1,
            minChargeUp: -1);

        settings.GetValidPropertiesInformation().Count.ShouldBe(3);
        settings.GetValidPropertiesInformation().ShouldContain((nameof(settings.MaxChargeUp), validMaxChargeUp));
    }

    [TestMethod]
    public void GetValidPropertiesInformation_NoValidDataForIntProperties_ReturnsOnlyBoolProperty()
    {
        var settings = new AgvSettingsReadModel(
            maxChargeUp: -1,
            batteryLevelCritical: -1,
            agvOfflineTimeoutMsWhileBusy: -1,
            agvOfflineTimeoutMsWhileIdle: -1,
            batteryLevelLow: -1,
            minChargeUp: -1);

        settings.GetValidPropertiesInformation().Count.ShouldBe(2);
        settings.GetValidPropertiesInformation().ShouldContain((nameof(settings.InfiniteCharging), false));
    }
}