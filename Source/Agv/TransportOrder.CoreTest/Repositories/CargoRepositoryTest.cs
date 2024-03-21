using System.Collections.ObjectModel;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Cargo = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv.Cargo;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

[TestClass]
public class CargoRepositoryTest
{
    [TestMethod]
    public async Task GetCargosForAgv_ValidData_ReturnsSelectedAgvCargos()
    {
        var fixture = new CargoRepositoryTestFixture();
        var agvRepository = fixture.AgvRepository;

        var agv = AgvGenerator.GenerateBusyAgv();
        await agvRepository.AddAgvAsync(agv);
        ReadOnlyCollection<Cargo> cargos = AgvCargoGenerator.GenerateCargos(1, agv.CorrelationId).ToList().AsReadOnly();
        await fixture.Repository.SetCargoAsync(agv.CorrelationId, cargos);

        IReadOnlyCollection<Cargo> selectedAgvCargoes = await fixture.Repository.GetCargosAsync(agv.CorrelationId);
        selectedAgvCargoes.ShouldNotBeNull();
        selectedAgvCargoes.Count.ShouldBe(cargos.Count);
        selectedAgvCargoes.First().LoadId.ShouldBe(cargos.First().LoadId);
    }

    [TestMethod]
    public async Task SetCargoForAgv_UpdateCargo_ReturnsSuccessful()
    {
        var fixture = new CargoRepositoryTestFixture();
        var agvRepository = fixture.AgvRepository;

        var agv = AgvGenerator.GenerateBusyAgv();
        await agvRepository.AddAgvAsync(agv);
        ReadOnlyCollection<Cargo> cargos = AgvCargoGenerator.GenerateCargos(1, agv.CorrelationId).ToList().AsReadOnly();

        await fixture.Repository.SetCargoAsync(agv.CorrelationId, cargos);

        ReadOnlyCollection<Cargo> updatedCargo = AgvCargoGenerator.GenerateCargos(1, agv.CorrelationId, "NewPos-").ToList().AsReadOnly();

        bool success = await fixture.Repository.SetCargoAsync(agv.CorrelationId, updatedCargo);
        success.ShouldBeTrue();

        IReadOnlyCollection<Cargo> selectedAgvCargoes = await fixture.Repository.GetCargosAsync(agv.CorrelationId);
        selectedAgvCargoes.ShouldNotBeNull();
        selectedAgvCargoes.Count.ShouldBe(updatedCargo.Count);
        selectedAgvCargoes.First().LoadId.ShouldBe(updatedCargo.First().LoadId);
        selectedAgvCargoes.First().LoadPosition.ShouldBe(updatedCargo.First().LoadPosition);
    }

    [TestMethod]
    public async Task SetCargoForAgv_AddAndUpdateCargo_ReturnsSuccessful()
    {
        var fixture = new CargoRepositoryTestFixture();
        var agvRepository = fixture.AgvRepository;
        
        var agv = AgvGenerator.GenerateBusyAgv();
        await agvRepository.AddAgvAsync(agv);

        ReadOnlyCollection<Cargo> cargos = AgvCargoGenerator.GenerateCargos(1, agv.CorrelationId).ToList().AsReadOnly();

        await fixture.Repository.SetCargoAsync(agv.CorrelationId, cargos);

        ReadOnlyCollection<Cargo> updatedCargo = AgvCargoGenerator.GenerateCargos(2, agv.CorrelationId, "NewPos-").ToList().AsReadOnly();
        bool success = await fixture.Repository.SetCargoAsync(agv.CorrelationId, updatedCargo);
        success.ShouldBeTrue();

        IReadOnlyCollection<Cargo> selectedAgvCargoes = await fixture.Repository.GetCargosAsync(agv.CorrelationId);
        selectedAgvCargoes.ShouldNotBeNull();
        selectedAgvCargoes.Count.ShouldBe(updatedCargo.Count);
        selectedAgvCargoes.Any(c => c.LoadPosition == updatedCargo[0].LoadPosition).ShouldBeTrue();
        selectedAgvCargoes.Any(c => c.LoadPosition == updatedCargo[1].LoadPosition).ShouldBeTrue();
    }

    [TestMethod]
    public async Task SetCargoForAgv_RemoveOneCargo_ReturnsSuccessful()
    {
        var fixture = new CargoRepositoryTestFixture();
        var agvRepository = fixture.AgvRepository;

        var agv = AgvGenerator.GenerateBusyAgv();
        await agvRepository.AddAgvAsync(agv);
        ReadOnlyCollection<Cargo> cargos = AgvCargoGenerator.GenerateCargos(2, agv.CorrelationId).ToList().AsReadOnly();
        await fixture.Repository.SetCargoAsync(agv.CorrelationId, cargos);

        ReadOnlyCollection<Cargo> onlyOneCargo = AgvCargoGenerator.GenerateCargos(1, agv.CorrelationId).ToList().AsReadOnly();

        bool success = await fixture.Repository.SetCargoAsync(agv.CorrelationId, onlyOneCargo);
        success.ShouldBeTrue();

        IReadOnlyCollection<Cargo> selectedAgvCargoes = await fixture.Repository.GetCargosAsync(agv.CorrelationId);
        selectedAgvCargoes.ShouldNotBeNull();
        selectedAgvCargoes.Count.ShouldBe(onlyOneCargo.Count);
        selectedAgvCargoes.Any(c => c.LoadId == onlyOneCargo.First().LoadId).ShouldBeTrue();
    }

    [TestMethod]
    public async Task SetCargoForAgv_RemoveAllCargoForAgv_ReturnsSuccessful()
    {
        var fixture = new CargoRepositoryTestFixture();
        var agvRepository = fixture.AgvRepository;

        var agv = AgvGenerator.GenerateBusyAgv();
        await agvRepository.AddAgvAsync(agv);
        ReadOnlyCollection<Cargo> cargos = AgvCargoGenerator.GenerateCargos(2, agv.CorrelationId).ToList().AsReadOnly();

        await fixture.Repository.SetCargoAsync(agv.CorrelationId, cargos);

        cargos = new List<Cargo>().AsReadOnly();
        bool success = await fixture.Repository.SetCargoAsync(agv.CorrelationId, cargos);
        success.ShouldBeTrue();

        IReadOnlyCollection<Cargo> selectedAgvCargoes = await fixture.Repository.GetCargosAsync(agv.CorrelationId);
        selectedAgvCargoes.ShouldNotBeNull();
        selectedAgvCargoes.Count.ShouldBe(0);
    }
}
