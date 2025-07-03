using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class AgvEfRepositoryTest
{
    [TestMethod]
    public void EmptyRepository_NoAGVAvailable()
    {
        var fixture = new EfAgvRepositoryTestFixture();

        fixture.Repository.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task OneIdleAdded_OneIdleAGVAvailable()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();

        await fixture.Add(agv);

        fixture.Repository.Count.ShouldBe(1);

        var readyAgv = (await fixture.Repository.GetIdleAgvsAsync()).First();
        readyAgv.CorrelationId.ShouldBe(agv.CorrelationId);
        readyAgv.CurrentAgvState.ShouldBe(agv.CurrentAgvState);
        readyAgv.SerialNumber.ShouldBe(agv.SerialNumber);
        readyAgv.IsOnline.ShouldBeTrue();
    }

    [TestMethod]
    public async Task TwoAdded_OneIdleAGVAvailable()
    {
        var fixture = new EfAgvRepositoryTestFixture();

        var idleAgv = AgvGenerator.GenerateIdleAgv();
        await fixture.Add(idleAgv);
        var chargingAgv = AgvGenerator.GenerateChargingAgv(1);
        await fixture.Add(chargingAgv);

        fixture.Repository.Count.ShouldBe(2);

        var readyAgv = (await fixture.Repository.GetIdleAgvsAsync()).First();
        readyAgv.CorrelationId.ShouldBe(idleAgv.CorrelationId);
        readyAgv.CurrentAgvState.ShouldBe(idleAgv.CurrentAgvState);
        readyAgv.SerialNumber.ShouldBe(idleAgv.SerialNumber);
    }

    [TestMethod]
    public async Task Verify_IAgvRepository_Interface_Methods()
    {
        var fixture = new EfAgvRepositoryTestFixture();

        var agv1 = AgvGenerator.GenerateIdleAgv();
        var agv2 = AgvGenerator.GenerateChargingAgv(1);

        await fixture.Add(agv1);
        await fixture.Add(agv2);

        var agvRepository = fixture.Repository;

        fixture.Repository.Count.ShouldBe(2);

        IEnumerable<Agv> allAgvs = await agvRepository.GetAllAgvsAsync();
        IEnumerable<Agv> readyAgvs = await agvRepository.GetIdleAgvsAsync();

        // ReSharper disable PossibleMultipleEnumeration
        allAgvs.Count().ShouldBe(2);
        readyAgvs.Count().ShouldBe(1);

        foreach (var actualAgv in await agvRepository.GetAllAgvsAsync())
        {
            var foundAgv = await agvRepository.GetAgvAsync(actualAgv.CorrelationId);
            foundAgv.ShouldNotBeNull();
            foundAgv.SerialNumber.ShouldBe(actualAgv.SerialNumber);
        }

        await Should.ThrowAsync<AgvNotFoundException>(agvRepository.GetAgvAsync(Guid.Empty));
    }

    [TestMethod]
    public async Task CheckIfAgvExistsAsync_ExistingAgv_ReturnsTrue()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        await fixture.Add(agv);

        bool agvExists = await fixture.Repository.CheckIfAgvExistsAsync(agv.CorrelationId);
        agvExists.ShouldBeTrue();
    }

    [TestMethod]
    public async Task CheckIfAgvExistsAsync_NonExistingAgv_ReturnsFalse()
    {
        var fixture = new EfAgvRepositoryTestFixture();

        bool agvExists = await fixture.Repository.CheckIfAgvExistsAsync("".ToGuid());
        agvExists.ShouldBeFalse();
    }

    [TestMethod]
    public async Task ThrowIfAgvNotExistsAsync_ExistingAgv_DoesNotThrow()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        await fixture.Add(agv);

        await fixture.Repository.ThrowIfAgvNotExistsAsync(agv.CorrelationId);
    }

    [TestMethod]
    public async Task ThrowIfAgvNotExistsAsync_NotExistingAgv_Throws()
    {
        var fixture = new EfAgvRepositoryTestFixture();

        await Should.ThrowAsync<AgvNotFoundException>(async () => await fixture.Repository.ThrowIfAgvNotExistsAsync("".ToGuid()));
    }

    [TestMethod]
    public async Task ThrowIfAgvsNotExistAsync_NoExistingAgv_Throws()
    {
        var fixture = new EfAgvRepositoryTestFixture();

        await Should.ThrowAsync<AgvsNotFoundException>(async () => await fixture.Repository.ThrowIfAgvsNotExistAsync(new List<Guid> { "".ToGuid() }));
    }

    [TestMethod]
    public async Task ThrowIfAgvsNotExistAsync_ExistingAgvButNotTheRequested_Throws()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        await fixture.Add(AgvGenerator.GenerateIdleAgv());

        await Should.ThrowAsync<AgvsNotFoundException>(async () => await fixture.Repository.ThrowIfAgvsNotExistAsync(new List<Guid> { "".ToGuid() }));
    }

    [TestMethod]
    public async Task ThrowIfAgvsNotExistAsync_AgvExisting_DoesNotThrow()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        await fixture.Add(agv);

        await fixture.Repository.ThrowIfAgvsNotExistAsync(new List<Guid> { agv.CorrelationId });
    }

    [TestMethod]
    public async Task GetReadyAgvsWithAtLeastLowBatteryLevel_ValidData_ReturnsAgvs()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        agv.SetOnline();
        await fixture.Add(agv);
        ICollection<Agv> agvs = await fixture.Repository.GetReadyAgvsWithAtLeastLowBatteryLevel();
        agvs.Single().CorrelationId.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task GetReadyAgvsWithAtLeastLowBatteryLevel_InUserMode_NoAgvReturned()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow, mode: new AgvMode(ModeType.UserMode));
        await fixture.Add(agv);
        ICollection<Agv> agvs = await fixture.Repository.GetReadyAgvsWithAtLeastLowBatteryLevel();
        agvs.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetReadyAgvsWithAtLeastLowBatteryLevel_InMaintenanceMode_NoAgvReturned()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow, mode: new AgvMode(ModeType.Maintenance));
        await fixture.Add(agv);
        ICollection<Agv> agvs = await fixture.Repository.GetReadyAgvsWithAtLeastLowBatteryLevel();
        agvs.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetChargingAgvsWithMinBatteryLevelAsync_AgvIsLocked_DoesNotReturnLockedAgv()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var lockedAgv = AgvGenerator.GenerateChargingAgv(batteryCharge: EfAgvSettingsProvider.DefaultSettings.MinChargeUp, mode: new AgvMode(ModeType.Locked));
        await fixture.Add(lockedAgv);

        ICollection<Agv> agvs = await fixture.Repository.GetChargingAgvsWithMinBatteryLevelAsync();
        agvs.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task SelectAgvsWithAboveCriticalBatteryLevel_NoAgvsToSelectFrom_ReturnsEmptyList()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv(batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        await fixture.Add(agv);

        (await fixture.Repository.SelectAgvsWithAboveCriticalBatteryLevel([])).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task SelectAgvsWithAboveCriticalBatteryLevel_ValidData_ReturnsAgvs()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var agvAboveCritical = AgvGenerator.GenerateIdleAgv(batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        var queueingAgv = AgvGenerator.GenerateIdleAgv(index:2,batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        queueingAgv.IsQueueing = true;
        var offlineAgv = AgvGenerator.GenerateIdleAgv(index:3, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        offlineAgv.GoOffline();
        var agvBelowCritical = AgvGenerator.GenerateIdleAgv(index:4, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical-1);
        var lockedAgv = AgvGenerator.GenerateIdleAgv(index:5,batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow, mode: new AgvMode(ModeType.Locked));
        var userModeAgv = AgvGenerator.GenerateIdleAgv(index:6, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow, mode: new AgvMode(ModeType.UserMode));
        var maintenanceModeAgv = AgvGenerator.GenerateIdleAgv(index:7,batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow, mode: new AgvMode(ModeType.Maintenance));

        await fixture.Add(agvBelowCritical);
        await fixture.Add(agvAboveCritical);
        await fixture.Add(queueingAgv);
        await fixture.Add(offlineAgv);
        await fixture.Add(lockedAgv);
        await fixture.Add(userModeAgv);
        await fixture.Add(maintenanceModeAgv);

        (await fixture.Repository.SelectAgvsWithAboveCriticalBatteryLevel([
            agvBelowCritical.CorrelationId, agvAboveCritical.CorrelationId, queueingAgv.CorrelationId,
            offlineAgv.CorrelationId, lockedAgv.CorrelationId, userModeAgv.CorrelationId,
            maintenanceModeAgv.CorrelationId
        ])).Select(a => a.CorrelationId).ToList().ShouldBeEquivalentTo(new List<Guid>{agvAboveCritical.CorrelationId});
    }

    [TestMethod]
    public async Task SelectAgvsWithAboveCriticalBatteryLevel_AgvNotAmongSelection_ReturnsEmpty()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var agvAboveCritical = AgvGenerator.GenerateIdleAgv(batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        var queueingAgv = AgvGenerator.GenerateIdleAgv(index: 2, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        queueingAgv.IsQueueing = true;
        var offlineAgv = AgvGenerator.GenerateIdleAgv(index: 3, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        offlineAgv.GoOffline();
        var agvBelowCritical = AgvGenerator.GenerateIdleAgv(index: 4, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical-1);
        var lockedAgv = AgvGenerator.GenerateIdleAgv(index: 5, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow, mode: new AgvMode(ModeType.Locked));
        var userModeAgv = AgvGenerator.GenerateIdleAgv(index: 6, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow, mode: new AgvMode(ModeType.UserMode));
        var maintenanceModeAgv = AgvGenerator.GenerateIdleAgv(index: 7, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow, mode: new AgvMode(ModeType.Maintenance));

        await fixture.Add(agvBelowCritical);
        await fixture.Add(agvAboveCritical);
        await fixture.Add(queueingAgv);
        await fixture.Add(offlineAgv);
        await fixture.Add(lockedAgv);
        await fixture.Add(userModeAgv);
        await fixture.Add(maintenanceModeAgv);

        (await fixture.Repository.SelectAgvsWithAboveCriticalBatteryLevel([
            agvBelowCritical.CorrelationId, queueingAgv.CorrelationId,
            offlineAgv.CorrelationId, lockedAgv.CorrelationId, userModeAgv.CorrelationId,
            maintenanceModeAgv.CorrelationId
        ])).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetChargingAgvsWithAtLeastMinChargeUpLevel_ValidData_ReturnsChargingAgvsWithMinBatteryLevel()
    {
        var agvLowLevel = AgvGenerator.GenerateChargingAgv(2, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        var agvMinLevel = AgvGenerator.GenerateChargingAgv(1, batteryCharge: EfAgvSettingsProvider.DefaultSettings.MinChargeUp + 1);
        var fixture = new EfAgvRepositoryTestFixture();
        await fixture.AddChargingAgv(agvLowLevel);
        await fixture.AddChargingAgv(agvMinLevel);

        ICollection<Agv> agvs = await fixture.Repository.GetChargingAgvsWithAtLeastMinChargeUpLevel();

        agvs.ShouldNotBeEmpty();
        agvs.Single().CorrelationId.ShouldBeEquivalentTo(agvMinLevel.CorrelationId);
    }

    [TestMethod]
    public async Task GetNotChargingAgvsWithCriticalBatteryLevel_ValidData_ReturnsNotChargingAgvsWithCriticalBatteryLevel()
    {
        var agvCriticalLevel = AgvGenerator.GenerateIdleAgv(2, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical);
        var chargingAgv = AgvGenerator.GenerateChargingAgv(1, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical);
        var fixture = new EfAgvRepositoryTestFixture();
        await fixture.Add(agvCriticalLevel);
        await fixture.AddChargingAgv(chargingAgv);

        ICollection<Agv> agvs = await fixture.Repository.GetNotChargingAgvsWithCriticalBatteryLevel();

        agvs.ShouldNotBeEmpty();
        agvs.Single().CorrelationId.ShouldBe(agvCriticalLevel.CorrelationId);
    }
}