using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

[TestClass]
public class EfAgvDataRepositoryTest
{
    [TestMethod]
    public async Task GetSelectedAgvData_ValidData_ReturnsSelectedData()
    {
        var fixture = new AgvDataRepositoryTestFixture();

        List<AgvData> specifications = Enumerable.Range(0, 4).Select(i =>
            new AgvData(Guid.NewGuid(), "Karis", "BF1", "ipv4 address",
                1.8, 120.0, 0.6, 0.8, 0.7, i,
                2.5, 1000, Array.Empty<AgvData.Sensor>())).ToList();
        await fixture.AddRangeAsync(specifications);

        List<Guid> correlationIds = specifications.Take(2).Select(s => s.CorrelationId).ToList();
        ICollection<AgvData> selectedAgvData = await fixture.Repository.GetSelectedAgvData(correlationIds);
        selectedAgvData.ShouldNotBeNull();
        selectedAgvData.Count.ShouldBe(correlationIds.Count);
        foreach (var actualData in selectedAgvData)
        {
            actualData.ShouldBeEquivalentTo(specifications.Single(s => s.CorrelationId == actualData.CorrelationId));
        }
    }

    [TestMethod]
    public async Task GetSelectedAgvData_UnknownCorrelationId_ReturnsKnownSelectedData()
    {
        var fixture = new AgvDataRepositoryTestFixture();

        List<AgvData> knownData = Enumerable.Range(0, 4).Select(i =>
            new AgvData(Guid.NewGuid(), "Karis", "BF1", "ipv4 address",
                1.8, 120.0, 0.6, 0.8, 0.7, i,
                2.5, 1000, Array.Empty<AgvData.Sensor>())).ToList();
        await fixture.AddRangeAsync(knownData);

        List<Guid> correlationIds = knownData.Select(s => s.CorrelationId).ToList();
        correlationIds.Add("Unknown".ToGuid());
        ICollection<AgvData> actualDataOfAgvs = await fixture.Repository.GetSelectedAgvData(correlationIds);
        actualDataOfAgvs.ShouldNotBeNull();
        actualDataOfAgvs.Count.ShouldBe(knownData.Count);
        foreach (var actualAgvData in actualDataOfAgvs)
        {
            actualAgvData.ShouldBeEquivalentTo(knownData.Single(s => s.CorrelationId == actualAgvData.CorrelationId));
        }
    }
}