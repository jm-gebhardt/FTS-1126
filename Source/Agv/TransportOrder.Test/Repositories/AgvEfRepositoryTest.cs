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
}