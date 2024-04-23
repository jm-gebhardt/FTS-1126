using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfAgvSettingsProviderTest
{
    [TestMethod]
    public async Task GetSettingsForAgvGroup_ValidData_ReturnsAgvSettingsReadModel()
    {
        var fixture = await new EfAgvSettingsProviderTestFixture().BuildAsync();
        var groupId = fixture.InMemorySettingsCache.ConcurrentDict.Keys.First();

        var settings = fixture.Repository.GetSettingsForAgvGroup(groupId);
        settings.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task GetSettingsForAgvGroup_UnknownGroup_ThrowsAgvSettingsNotFoundException()
    {
        var fixture = await new EfAgvSettingsProviderTestFixture().BuildAsync();
        Should.Throw<AgvSettingsNotFoundException>(() => fixture.Repository.GetSettingsForAgvGroup("unknown".ToGuid()));
    }

    [TestMethod]
    public async Task AddOrUpdateSettingsForAgvGroup_AddGroupFromCompleteNewSettings_GroupAddedWithNewSettings()
    {
        var fixture = await new EfAgvSettingsProviderTestFixture().BuildAsync();

        const GroupType groupType = GroupType.SerialNumber;
        const string groupValue = "an agv serial number";
        var agvSettings = new AgvSettingsReadModel(
            agvOfflineTimeoutMsWhileBusy: 12,
            agvOfflineTimeoutMsWhileIdle: 12,
            maxChargeUp: 100,
            minChargeUp: 20,
            batteryLevelLow: 20,
            batteryLevelCritical: 1);

        await fixture.Repository.AddOrUpdateSettingsForAgvGroup(agvSettings, groupType, groupValue);

        var addedGroup = (await fixture.GetAddedAgvGroups(groupType, groupValue)).Single();
        addedGroup.GroupType.ShouldBe(groupType);
        addedGroup.Identifier.ShouldBe(groupValue);
        addedGroup.AgvSetting.BatteryLevelCritical.Value.ShouldBe(agvSettings.BatteryLevelCritical.ToString());
        addedGroup.AgvSetting.BatteryLevelLow.Value.ShouldBe(agvSettings.BatteryLevelLow.ToString());
        addedGroup.AgvSetting.MinChargeUp.Value.ShouldBe(agvSettings.MinChargeUp.ToString());
        addedGroup.AgvSetting.MaxChargeUp.Value.ShouldBe(agvSettings.MaxChargeUp.ToString());
        addedGroup.AgvSetting.AgvOfflineTimeoutMsWhileBusy.Value.ShouldBe(agvSettings.AgvOfflineTimeoutMsWhileBusy.ToString());
        addedGroup.AgvSetting.AgvOfflineTimeoutMsWhileIdle.Value.ShouldBe(agvSettings.AgvOfflineTimeoutMsWhileIdle.ToString());

        fixture.InMemorySettingsCache.ConcurrentDict[addedGroup.Id].ShouldBeEquivalentTo(agvSettings);
    }

    [TestMethod]
    public async Task AddOrUpdateSettingsForAgvGroup_AddGroupFromSomeNewSettings_GroupAddedWithNewAndDefaultSettings()
    {
        var fixture = await new EfAgvSettingsProviderTestFixture().BuildAsync();
        var defaultGroupId = fixture.InMemorySettingsCache.ConcurrentDict.Keys.First();
        var defaultSettings = fixture.InMemorySettingsCache.ConcurrentDict[defaultGroupId];
        
        const GroupType groupType = GroupType.SerialNumber;
        const string groupValue = "an agv serial number";
        var agvSettings = new AgvSettingsReadModel(
            agvOfflineTimeoutMsWhileBusy: 12,
            maxChargeUp: 100,
            batteryLevelCritical: 1);

        await fixture.Repository.AddOrUpdateSettingsForAgvGroup(agvSettings, groupType, groupValue);
        var defaultGroup = await fixture.Repository.GetSettingsGroupForAgv();

        var addedGroup = (await fixture.GetAddedAgvGroups(groupType, groupValue)).Single();
        var inMemorySettings = fixture.InMemorySettingsCache.ConcurrentDict[addedGroup.Id];

        addedGroup.GroupType.ShouldBe(groupType);
        addedGroup.Identifier.ShouldBe(groupValue);
        addedGroup.AgvSetting.BatteryLevelCritical.Value.ShouldBe(agvSettings.BatteryLevelCritical.ToString());
        inMemorySettings.BatteryLevelCritical.ShouldBe(agvSettings.BatteryLevelCritical);
        addedGroup.AgvSetting.MaxChargeUp.Value.ShouldBe(agvSettings.MaxChargeUp.ToString());
        inMemorySettings.MaxChargeUp.ShouldBe(agvSettings.MaxChargeUp);
        addedGroup.AgvSetting.AgvOfflineTimeoutMsWhileBusy.Value.ShouldBe(agvSettings.AgvOfflineTimeoutMsWhileBusy.ToString());
        inMemorySettings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(agvSettings.AgvOfflineTimeoutMsWhileBusy);

        addedGroup.AgvSetting.MinChargeUp.Value.ShouldBe(defaultSettings.MinChargeUp.ToString());
        addedGroup.AgvSetting.MinChargeUp.Id.ShouldBe(defaultGroup.AgvSetting.MinChargeUp.Id);
        inMemorySettings.MinChargeUp.ShouldBe(defaultSettings.MinChargeUp);
        addedGroup.AgvSetting.AgvOfflineTimeoutMsWhileIdle.Value.ShouldBe(defaultSettings.AgvOfflineTimeoutMsWhileIdle.ToString());
        addedGroup.AgvSetting.AgvOfflineTimeoutMsWhileIdle.Id.ShouldBe(defaultGroup.AgvSetting.AgvOfflineTimeoutMsWhileIdle.Id);
        inMemorySettings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(defaultSettings.AgvOfflineTimeoutMsWhileIdle);
        addedGroup.AgvSetting.BatteryLevelLow.Value.ShouldBe(defaultSettings.BatteryLevelLow.ToString());
        addedGroup.AgvSetting.BatteryLevelLow.Id.ShouldBe(defaultGroup.AgvSetting.BatteryLevelLow.Id);
        inMemorySettings.BatteryLevelLow.ShouldBe(defaultSettings.BatteryLevelLow);
    }

    [TestMethod]
    public async Task AddOrUpdateSettingsForAgvGroup_Update_GroupUpdated()
    {
        var oldSettings = EfAgvSettingsProvider.DefaultSettings;
        const GroupType groupType = GroupType.SerialNumber;
        const string groupValue = "an agv serial number";
        var fixture = await new EfAgvSettingsProviderTestFixture()
            .WithAgvSettingsGroup(oldSettings, groupType, groupValue)
            .BuildAsync();

        var updatedSettings = new AgvSettingsReadModel(
        batteryLevelCritical: 12);

        await fixture.Repository.AddOrUpdateSettingsForAgvGroup(updatedSettings, groupType, groupValue);
        var updatedGroup = (await fixture.GetAddedAgvGroups(groupType, groupValue)).Single();
        var inMemorySettings = fixture.InMemorySettingsCache.ConcurrentDict[updatedGroup.Id];

        updatedGroup.GroupType.ShouldBe(groupType);
        updatedGroup.Identifier.ShouldBe(groupValue);
        updatedGroup.AgvSetting.BatteryLevelCritical.Value.ShouldBe(updatedSettings.BatteryLevelCritical.ToString());
        inMemorySettings.BatteryLevelCritical.ShouldBe(updatedSettings.BatteryLevelCritical);
        
        updatedGroup.AgvSetting.MaxChargeUp.Value.ShouldBe(oldSettings.MaxChargeUp.ToString());
        inMemorySettings.MaxChargeUp.ShouldBe(oldSettings.MaxChargeUp);
        updatedGroup.AgvSetting.AgvOfflineTimeoutMsWhileBusy.Value.ShouldBe(oldSettings.AgvOfflineTimeoutMsWhileBusy.ToString());
        inMemorySettings.AgvOfflineTimeoutMsWhileBusy.ShouldBe(oldSettings.AgvOfflineTimeoutMsWhileBusy);
        updatedGroup.AgvSetting.MinChargeUp.Value.ShouldBe(oldSettings.MinChargeUp.ToString());
        inMemorySettings.MinChargeUp.ShouldBe(oldSettings.MinChargeUp);
        updatedGroup.AgvSetting.AgvOfflineTimeoutMsWhileIdle.Value.ShouldBe(oldSettings.AgvOfflineTimeoutMsWhileIdle.ToString());
        inMemorySettings.AgvOfflineTimeoutMsWhileIdle.ShouldBe(oldSettings.AgvOfflineTimeoutMsWhileIdle);
        updatedGroup.AgvSetting.BatteryLevelLow.Value.ShouldBe(oldSettings.BatteryLevelLow.ToString());
        inMemorySettings.BatteryLevelLow.ShouldBe(oldSettings.BatteryLevelLow);
    }

    [TestMethod]
    public async Task GetSettingsGroupForAgv_NoAgvProvided_ReturnsDefaultGroup()
    {
        var defaultSettings = EfAgvSettingsProvider.DefaultSettings;
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = await new EfAgvSettingsProviderTestFixture()
            .WithAgvSettingsGroup(defaultSettings, GroupType.SerialNumber, agv.SerialNumber)
            .BuildAsync();

        var agvGroup = await fixture.Repository.GetSettingsGroupForAgv();
        agvGroup.GroupType.ShouldBe(GroupType.All);
    }

    [TestMethod]
    public async Task GetSettingsGroupForAgv_AgvProvided_SettingsGroupForSerialNumberExists_ReturnsSerialNumberGroup()
    {
        var defaultSettings = EfAgvSettingsProvider.DefaultSettings;
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = await new EfAgvSettingsProviderTestFixture()
            .WithAgvSettingsGroup(defaultSettings, GroupType.SerialNumber, agv.SerialNumber)
            .BuildAsync();

        var agvGroup = await fixture.Repository.GetSettingsGroupForAgv(agv);
        agvGroup.GroupType.ShouldBe(GroupType.SerialNumber);
        agvGroup.Identifier.ShouldBe(agv.SerialNumber);
    }

    [TestMethod]
    public async Task GetSettingsGroupForAgv_AgvProvided_NoSettingsGroupForSerialNumberExists_ReturnsDefault()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = await new EfAgvSettingsProviderTestFixture()
            .BuildAsync();

        var agvGroup = await fixture.Repository.GetSettingsGroupForAgv(agv);
        agvGroup.GroupType.ShouldBe(GroupType.All);
    }

    [TestMethod]
    public async Task InitializeSettingsGroups_NoGroups_CreatesDefaultGroup()
    {
        var fixture = await new EfAgvSettingsProviderTestFixture()
            .BuildAsync();
        await fixture.ResetDb();

        await Should.ThrowAsync<AgvSettingsNotFoundException>(async () => await fixture.Repository.GetSettingsGroupForAgv());

        await fixture.Repository.InitializeSettingsGroups();

        var defaultGroup = await fixture.Repository.GetSettingsGroupForAgv();
        defaultGroup.GroupType.ShouldBe(GroupType.All);
        fixture.InMemorySettingsCache.ConcurrentDict[defaultGroup.Id].ShouldBe(defaultGroup.AgvSetting.ToReadModel());
    }

    [TestMethod]
    public async Task InitializeSettingsGroups_ExistingGroups_ReloadedIntoMemory()
    {
        var defaultSettings = EfAgvSettingsProvider.DefaultSettings;
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = await new EfAgvSettingsProviderTestFixture()
            .WithAgvSettingsGroup(defaultSettings, GroupType.SerialNumber, agv.SerialNumber)
            .BuildAsync();

        await fixture.Repository.InitializeSettingsGroups();

        var defaultGroup = await fixture.Repository.GetSettingsGroupForAgv();
        defaultGroup.GroupType.ShouldBe(GroupType.All);
        fixture.InMemorySettingsCache.ConcurrentDict[defaultGroup.Id].ShouldBe(defaultGroup.AgvSetting.ToReadModel());

        var existingGroup = await fixture.Repository.GetSettingsGroupForAgv(agv);
        existingGroup.GroupType.ShouldBe(GroupType.SerialNumber);
        existingGroup.Identifier.ShouldBe(agv.SerialNumber);
        fixture.InMemorySettingsCache.ConcurrentDict[existingGroup.Id].ShouldBe(existingGroup.AgvSetting.ToReadModel());
    }

    [TestMethod]
    public async Task ResetSettingsPropertyToDefaultAsync_ValidData_PropertyReset()
    {
        var defaultSettings = EfAgvSettingsProvider.DefaultSettings;
        var agvSettings = new AgvSettingsReadModel(
            agvOfflineTimeoutMsWhileIdle: EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileIdle,
            agvOfflineTimeoutMsWhileBusy: EfAgvSettingsProvider.DefaultSettings.AgvOfflineTimeoutMsWhileBusy,
            batteryLevelCritical: 12,
            batteryLevelLow: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow,
            minChargeUp: EfAgvSettingsProvider.DefaultSettings.MinChargeUp,
            maxChargeUp: EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = await new EfAgvSettingsProviderTestFixture()
            .WithAgvSettingsGroup(agvSettings, GroupType.SerialNumber, agv.SerialNumber)
            .BuildAsync();

        var agvSettingsGroup = await fixture.Repository.GetSettingsGroupForAgv(agv);
        var defaultSettingsGroup = await fixture.Repository.GetSettingsGroupForAgv();

        agvSettingsGroup.AgvSetting.BatteryLevelCritical.ShouldNotBe(defaultSettingsGroup.AgvSetting.BatteryLevelCritical);
        fixture.InMemorySettingsCache.ConcurrentDict[agvSettingsGroup.Id].ShouldBeEquivalentTo(agvSettings);

        await fixture.Repository.ResetSettingPropertyToDefaultAsync(agvSettingsGroup.Id, nameof(agvSettings.BatteryLevelCritical));
        
        agvSettingsGroup = await fixture.Repository.GetSettingsGroupForAgv(agv);
        agvSettingsGroup.AgvSetting.BatteryLevelCritical.ShouldBeEquivalentTo(defaultSettingsGroup.AgvSetting.BatteryLevelCritical);
        // As BatteryLevelCritical was the only one differing from the defaultSettings:
        fixture.InMemorySettingsCache.ConcurrentDict[agvSettingsGroup.Id].ShouldBeEquivalentTo(defaultSettings);
    }

    [TestMethod]
    public async Task ResetSettingsPropertyToDefaultAsync_GroupNotFound_ThrowsAgvSettingsNotFoundException()
    {
        var fixture = await new EfAgvSettingsProviderTestFixture()
            .BuildAsync();

        await Should.ThrowAsync<AgvSettingsNotFoundException>( () => fixture.Repository.ResetSettingPropertyToDefaultAsync("unknown".ToGuid(), "a known property"));
    }
}