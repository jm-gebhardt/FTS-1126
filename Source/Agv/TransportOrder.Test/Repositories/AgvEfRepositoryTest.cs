using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Repositories;

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
        readyAgv.IsOnline.ShouldBeFalse();
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
    public async Task GetReadyAgvsAsync_WithoutAgvStatus_DoesNotReturnAgv()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var readyAgv = AgvGenerator.GenerateReadyAgv();
        readyAgv.AgvStatus = null;

        await fixture.Add(readyAgv);

        (await fixture.Repository.GetReadyAgvsAsync()).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetReadyAgvsAsync_WithError_DoesNotReturnAgv()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var readyAgv = AgvGenerator.GenerateReadyAgv();
        readyAgv.HasError = true;

        await fixture.Add(readyAgv);

        (await fixture.Repository.GetReadyAgvsAsync()).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetReadyAgvsAsync_IsLocked_DoesNotReturnAgv()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var readyAgv = AgvGenerator.GenerateReadyAgv();
        readyAgv.Lock();

        await fixture.Add(readyAgv);

        readyAgv.IsLocked.ShouldBeTrue();
        (await fixture.Repository.GetReadyAgvsAsync()).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetReadyAgvsAsync_NotInIdleState_DoesNotReturnAgv()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var readyAgv = AgvGenerator.GenerateReadyAgv();
        readyAgv.TryTransitionToBusy(true);
        await fixture.Add(readyAgv);

        readyAgv.CurrentAgvState.ShouldBe(AgvState.Busy);

        (await fixture.Repository.GetReadyAgvsAsync()).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetReadyAgvsAsync_WithoutCertainBatteryLevel_DoesNotReturnAgv()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var readyAgv = AgvGenerator.GenerateReadyAgv(batteryCharge: 54);

        await fixture.Add(readyAgv);

        (await fixture.Repository.GetReadyAgvsAsync(55)).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetReadyAgvsAsync_WithCertainBatteryLevel_ReturnAgv()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var readyAgv = AgvGenerator.GenerateReadyAgv(batteryCharge: 55);

        await fixture.Add(readyAgv);

        (await fixture.Repository.GetReadyAgvsAsync(55)).Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetReadyAgvsAsync_ReadyAgv_ReturnsAgv()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var readyAgv = AgvGenerator.GenerateReadyAgv();

        await fixture.Add(readyAgv);

        (await fixture.Repository.GetReadyAgvsAsync()).Count.ShouldBe(1);
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

        IEnumerable<TransportOrder.Domain.Models.Agv.Agv> allAgvs = await agvRepository.GetAllAgvsAsync();
        IEnumerable<TransportOrder.Domain.Models.Agv.Agv> readyAgvs = await agvRepository.GetIdleAgvsAsync();

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
        await fixture.Add(agv);
        ICollection<TransportOrder.Domain.Models.Agv.Agv> agvs = await fixture.Repository.GetReadyAgvsWithAtLeastLowBatteryLevel();
        agvs.Single().CorrelationId.ShouldBe(agv.CorrelationId);
    }
}