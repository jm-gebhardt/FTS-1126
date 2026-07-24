using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Helper;
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
    public async Task SelectInfinitelyChargingAgvsWithAboveMinChargeUpBatteryLevel_ValidData_ReturnsAgvs()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var agvAboveMinChargeUp = AgvGenerator.GenerateIdleAgv(batteryCharge: EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);
        var queueingAgv = AgvGenerator.GenerateIdleAgv(index: 2, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        queueingAgv.IsQueueing = true;
        var offlineAgv = AgvGenerator.GenerateIdleAgv(index: 3, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelLow);
        offlineAgv.GoOffline();
        var agvBelowMinChargeUp = AgvGenerator.GenerateIdleAgv(index: 4, batteryCharge: EfAgvSettingsProvider.DefaultSettings.MinChargeUp-1);
        var lockedAgv = AgvGenerator.GenerateIdleAgv(index: 5, batteryCharge: EfAgvSettingsProvider.DefaultSettings.MaxChargeUp, mode: new AgvMode(ModeType.Locked));
        var userModeAgv = AgvGenerator.GenerateIdleAgv(index: 6, batteryCharge: EfAgvSettingsProvider.DefaultSettings.MaxChargeUp, mode: new AgvMode(ModeType.UserMode));
        var maintenanceModeAgv = AgvGenerator.GenerateIdleAgv(index: 7, batteryCharge: EfAgvSettingsProvider.DefaultSettings.MaxChargeUp, mode: new AgvMode(ModeType.Maintenance));
        var infiniteChargingAgvAboveMinChargeUp = AgvGenerator.GenerateIdleAgv(8, batteryCharge: EfAgvSettingsProvider.DefaultSettings.MaxChargeUp);

        await fixture.Add(agvBelowMinChargeUp);
        await fixture.Add(infiniteChargingAgvAboveMinChargeUp, true);
        await fixture.Add(agvAboveMinChargeUp);
        await fixture.Add(queueingAgv);
        await fixture.Add(offlineAgv);
        await fixture.Add(lockedAgv);
        await fixture.Add(userModeAgv);
        await fixture.Add(maintenanceModeAgv);

        (await fixture.Repository.SelectInfinitelyChargingAgvsWithAboveMinChargeUpBatteryLevel([
            agvBelowMinChargeUp.CorrelationId, agvAboveMinChargeUp.CorrelationId, queueingAgv.CorrelationId,
            offlineAgv.CorrelationId, lockedAgv.CorrelationId, userModeAgv.CorrelationId,
            maintenanceModeAgv.CorrelationId, infiniteChargingAgvAboveMinChargeUp.CorrelationId
        ])).Select(a => a.CorrelationId).ToList().ShouldBeEquivalentTo(new List<Guid> { infiniteChargingAgvAboveMinChargeUp.CorrelationId });
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

        ICollection<Agv> agvs = await fixture.Repository.GetAgvsOnChargerReachableByAgvWithAtLeastMinChargeUpLevel();

        agvs.ShouldNotBeEmpty();
        agvs.Single().CorrelationId.ShouldBeEquivalentTo(agvMinLevel.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvsOnChargerReachableByAgvWithAtLeastMinChargeUpLevel_AgvCorrelationIdProvided_FiltersToReachableAgvsOnly()
    {
        var reachableAgv = AgvGenerator.GenerateChargingAgv(1, lastNodeId: "C-1", batteryCharge: EfAgvSettingsProvider.DefaultSettings.MinChargeUp + 1);
        var unreachableAgv = AgvGenerator.GenerateChargingAgv(2, lastNodeId: "C-2", batteryCharge: EfAgvSettingsProvider.DefaultSettings.MinChargeUp + 1);
        var requestingAgv = AgvGenerator.GenerateIdleAgv(3);
        var fixture = new EfAgvRepositoryTestFixture();
        await fixture.AddChargingAgv(reachableAgv);
        await fixture.AddChargingAgv(unreachableAgv);
        await fixture.AddWithGraph(requestingAgv, new GraphNode("C-1".ToGuid(), "C-1"));

        ICollection<Agv> agvs = await fixture.Repository.GetAgvsOnChargerReachableByAgvWithAtLeastMinChargeUpLevel(requestingAgv.CorrelationId);

        agvs.ShouldNotBeEmpty();
        agvs.Single().CorrelationId.ShouldBe(reachableAgv.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvsOnChargerReachableByAgvWithAtLeastMinChargeUpLevel_NoGraphForGivenAgv_ReturnsEmpty()
    {
        var chargingAgv = AgvGenerator.GenerateChargingAgv(1, batteryCharge: EfAgvSettingsProvider.DefaultSettings.MinChargeUp + 1);
        var fixture = new EfAgvRepositoryTestFixture();
        await fixture.AddChargingAgv(chargingAgv);

        ICollection<Agv> agvs = await fixture.Repository.GetAgvsOnChargerReachableByAgvWithAtLeastMinChargeUpLevel(Guid.NewGuid());

        agvs.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAgvsOnChargerReachableByAgvWithAtLeastMinChargeUpLevel_ChargeLevelAndAgvProvided_FiltersByChargeAndReachability()
    {
        var reachableAgv = AgvGenerator.GenerateChargingAgv(1, lastNodeId: "C-1", batteryCharge: 20);
        var unreachableAgv = AgvGenerator.GenerateChargingAgv(2, lastNodeId: "C-2", batteryCharge: 20);
        var requestingAgv = AgvGenerator.GenerateIdleAgv(3);
        var fixture = new EfAgvRepositoryTestFixture();
        await fixture.AddChargingAgv(reachableAgv);
        await fixture.AddChargingAgv(unreachableAgv);
        await fixture.AddWithGraph(requestingAgv, new GraphNode("C-1".ToGuid(), "C-1"));

        ICollection<Agv> agvs = await fixture.Repository.GetAgvsOnChargerReachableByAgvWithAtLeastMinChargeUpLevel(0, requestingAgv.CorrelationId);

        agvs.ShouldNotBeEmpty();
        agvs.Single().CorrelationId.ShouldBe(reachableAgv.CorrelationId);
    }

    [TestMethod]
    public async Task GetNotChargingAgvsWithCriticalBatteryLevelThatCanReachCharger_ValidData_ReturnsNotChargingAgvsWithCriticalBatteryLevel()
    {
        var agvCriticalLevel = AgvGenerator.GenerateIdleAgv(2, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical);
        var chargingAgv = AgvGenerator.GenerateChargingAgv(1, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical);
        var fixture = new EfAgvRepositoryTestFixture();
        await fixture.Add(agvCriticalLevel);
        await fixture.AddChargingAgv(chargingAgv);

        ICollection<Agv> agvs = await fixture.Repository.GetNotChargingAgvsWithCriticalBatteryLevelThatCanReachCharger(null);

        agvs.ShouldNotBeEmpty();
        agvs.Single().CorrelationId.ShouldBe(agvCriticalLevel.CorrelationId);
    }

    [TestMethod]
    public async Task GetNotChargingAgvsWithCriticalBatteryLevelThatCanReachCharger_ChargerProvided_FiltersToAgvsThatCanReachIt()
    {
        var chargerNode = new GraphNode("CHARGER".ToGuid(), "CHARGER");
        var reachableAgv = AgvGenerator.GenerateIdleAgv(1, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical);
        var unreachableAgv = AgvGenerator.GenerateIdleAgv(2, batteryCharge: EfAgvSettingsProvider.DefaultSettings.BatteryLevelCritical);
        var fixture = new EfAgvRepositoryTestFixture();
        await fixture.AddWithGraph(reachableAgv, chargerNode);
        await fixture.Add(unreachableAgv);

        ICollection<Agv> agvs = await fixture.Repository.GetNotChargingAgvsWithCriticalBatteryLevelThatCanReachCharger(chargerNode.Id);

        agvs.ShouldNotBeEmpty();
        agvs.Single().CorrelationId.ShouldBe(reachableAgv.CorrelationId);
    }
}