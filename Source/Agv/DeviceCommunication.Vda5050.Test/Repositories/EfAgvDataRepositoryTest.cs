using Gebhardt.Acc.Common.Testing.Helpers;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Repositories;

[TestClass]
public class EfAgvDataRepositoryTest
{
    [TestMethod]
    public async Task GetAgvDataAsync_ValidCorrelationId_ReturnsAgvData()
    {
        var fixture = new AgvDataRepositoryTestFixture();

        var agvData = AgvDataGenerator.GenerateAgvData();
        await fixture.AddAsync(agvData);

        var result = await fixture.Repository.GetAgvDataAsync(agvData.AgvCorrelationId);

        result.ShouldNotBeNull();
        result.ShouldBeEquivalentTo(agvData);
    }

    [TestMethod]
    public async Task GetAgvDataAsync_UnknownCorrelationId_ReturnsNull()
    {
        var fixture = new AgvDataRepositoryTestFixture();

        var agvData = AgvDataGenerator.GenerateAgvData();
        await fixture.AddAsync(agvData);

        var result = await fixture.Repository.GetAgvDataAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task AddOrUpdateAsync_NewAgvData_StoresDataInRepository()
    {
        var fixture = new AgvDataRepositoryTestFixture();

        var agvData = AgvDataGenerator.GenerateAgvData();
        await fixture.Repository.AddOrUpdateAsync(agvData);

        var result = await fixture.Repository.GetAgvDataAsync(agvData.AgvCorrelationId);

        result.ShouldNotBeNull();
        result.ShouldBeEquivalentTo(agvData);
    }

    [TestMethod]
    public async Task AddOrUpdateAsync_ExistingAgvData_UpdatesDataInRepository()
    {
        var fixture = new AgvDataRepositoryTestFixture();

        var agvData = AgvDataGenerator.GenerateAgvData();
        await fixture.Repository.AddOrUpdateAsync(agvData);

        var updatedAgvData = AgvDataGenerator.GenerateAgvData(
            correlationId: agvData.AgvCorrelationId,
            width: 3.0,
            maxVelocityStraight: 1.2);
        await fixture.Repository.AddOrUpdateAsync(updatedAgvData);

        var result = await fixture.Repository.GetAgvDataAsync(agvData.AgvCorrelationId);

        result.ShouldNotBeNull();
        result.ShouldBeEquivalentTo(updatedAgvData);
    }
}