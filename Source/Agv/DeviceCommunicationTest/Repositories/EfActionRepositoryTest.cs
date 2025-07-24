using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Repositories;

[TestClass]
public class EfActionRepositoryTest
{
    [TestMethod]
    public async Task AddVehicleAndInternalOrderIdMapping_ExistingMapping_MappingUpdated()
    {
        var vehicleOrderId = Guid.NewGuid();
        var internalOrderId = Guid.NewGuid();
        var mapping = new OrderIdMapping(vehicleOrderId, Guid.NewGuid());

        var fixture = new EfActionRepositoryTestFixture();
        await fixture.AddAsync(mapping);
        await fixture.Repository.SaveAsync();

        await fixture.Repository.AddVehicleAndInternalOrderIdMapping(vehicleOrderId, internalOrderId);

        (await fixture.Repository.GetByIdAsync(vehicleOrderId)).InternalOrderId.ShouldBe(internalOrderId);
    }

    [TestMethod]
    public async Task AddVehicleAndInternalOrderIdMapping_NewMapping_MappingAdded()
    {
        var vehicleOrderId = Guid.NewGuid();
        var internalOrderId = Guid.NewGuid();

        var fixture = new EfActionRepositoryTestFixture();

        await fixture.Repository.AddVehicleAndInternalOrderIdMapping(vehicleOrderId, internalOrderId);

        (await fixture.Repository.GetByIdAsync(vehicleOrderId)).InternalOrderId.ShouldBe(internalOrderId);
    }

    [TestMethod]
    public async Task GetInternalOrderId_ValidData_ReturnsInternalOrderId()
    {
        var vehicleOrderId = Guid.NewGuid();
        var internalOrderId = Guid.NewGuid();
        var mapping = new OrderIdMapping(vehicleOrderId, internalOrderId);

        var fixture = new EfActionRepositoryTestFixture();
        await fixture.AddAsync(mapping);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.GetInternalOrderId(vehicleOrderId)).ShouldBe(internalOrderId);
    }

    [TestMethod]
    public async Task GetInternalOrderId_MappingDoesNotExist_ReturnsNull()
    {
        var vehicleOrderId = Guid.NewGuid();

        var fixture = new EfActionRepositoryTestFixture();

        (await fixture.Repository.GetInternalOrderId(vehicleOrderId)).ShouldBeNull();
    }

    [TestMethod]
    public async Task GetVehicleOrderId_ValidData_ReturnsVehicleOrderId()
    {
        var vehicleOrderId = Guid.NewGuid();
        var internalOrderId = Guid.NewGuid();
        var mapping = new OrderIdMapping(vehicleOrderId, internalOrderId);

        var fixture = new EfActionRepositoryTestFixture();
        await fixture.AddAsync(mapping);
        await fixture.Repository.SaveAsync();

        (await fixture.Repository.GetVehicleOrderId(internalOrderId)).ShouldBe(vehicleOrderId);
    }

    [TestMethod]
    public async Task GetVehicleOrderId_MappingDoesNotExist_ReturnsNull()
    {
        var internalOrderId = Guid.NewGuid();

        var fixture = new EfActionRepositoryTestFixture();

        (await fixture.Repository.GetVehicleOrderId(internalOrderId)).ShouldBeNull();
    }

    [TestMethod]
    public async Task RemoveVehicleAndInternalOrderIdMapping_ValidData_RemovesMapping()
    {
        var vehicleOrderId = Guid.NewGuid();
        var internalOrderId = Guid.NewGuid();
        var mapping = new OrderIdMapping(vehicleOrderId, internalOrderId);

        var fixture = new EfActionRepositoryTestFixture();
        await fixture.AddAsync(mapping);
        await fixture.Repository.SaveAsync();

        await fixture.Repository.RemoveVehicleAndInternalOrderIdMapping(vehicleOrderId);
        (await fixture.Repository.GetInternalOrderId(vehicleOrderId)).ShouldBeNull();
    }
}