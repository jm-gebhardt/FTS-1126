using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Repositories;

[TestClass]
public class EfAgvRepositoryTest
{
    [TestMethod]
    public async Task GetAgvAsync_InCache_ReturnsAgv()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var fixture = new AgvRepositoryTestFixture()
            .WithCachedAgv(agv);

        (await fixture.Repository.GetAgvAsync(agv.CorrelationId))?.CorrelationId.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvAsync_InDb_ReturnsAgv()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var fixture = new AgvRepositoryTestFixture();

        await fixture.Repository.AddOrUpdateAsync(agv);

        (await fixture.Repository.GetAgvAsync(agv.CorrelationId))?.CorrelationId.ShouldBe(agv.CorrelationId);
    }

    [TestMethod]
    public async Task GetAgvAsync_NoAgv_ReturnsNull()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var fixture = new AgvRepositoryTestFixture();

        (await fixture.Repository.GetAgvAsync(agv.CorrelationId)).ShouldBeNull();
    }
}
