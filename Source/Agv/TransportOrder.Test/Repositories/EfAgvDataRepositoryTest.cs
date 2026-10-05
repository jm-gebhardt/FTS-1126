using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfAgvDataRepositoryTest
{
    [TestMethod]
    public async Task GetSelectedAgvData_ValidData_ReturnsSelectedData()
    {
        var fixture = new AgvDataRepositoryTestFixture();

        var specifications = Enumerable.Range(0, 4).Select(_ => AgvDataGenerator.GenerateAgvData()).ToList();
        await fixture.AddRangeAsync(specifications);

        var correlationIds = specifications.Take(2).Select(s => s.AgvCorrelationId).ToList();
        var selectedAgvData = await fixture.Repository.GetSelectedAgvDataAsync(correlationIds);
        selectedAgvData.ShouldNotBeNull();
        selectedAgvData.Count.ShouldBe(correlationIds.Count);
        foreach (var actualData in selectedAgvData)
        {
            actualData.ShouldBeEquivalentTo(specifications.Single(s => s.AgvCorrelationId == actualData.AgvCorrelationId));
        }
    }

    [TestMethod]
    public async Task GetSelectedAgvData_UnknownCorrelationId_ReturnsKnownSelectedData()
    {
        var fixture = new AgvDataRepositoryTestFixture();

        var knownData = Enumerable.Range(0, 4).Select(_ => AgvDataGenerator.GenerateAgvData()).ToList();
        await fixture.AddRangeAsync(knownData);

        var correlationIds = knownData.Select(s => s.AgvCorrelationId).ToList();
        correlationIds.Add("Unknown".ToGuid());
        var actualDataOfAgvs = await fixture.Repository.GetSelectedAgvDataAsync(correlationIds);
        actualDataOfAgvs.ShouldNotBeNull();
        actualDataOfAgvs.Count.ShouldBe(knownData.Count);
        foreach (var actualAgvData in actualDataOfAgvs)
        {
            actualAgvData.ShouldBeEquivalentTo(knownData.Single(s => s.AgvCorrelationId == actualAgvData.AgvCorrelationId));
        }
    }
}