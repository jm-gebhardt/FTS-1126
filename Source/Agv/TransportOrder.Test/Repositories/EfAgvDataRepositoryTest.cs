using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Repositories;

[TestClass]
public class EfAgvDataRepositoryTest
{
    [TestMethod]
    public async Task GetSelectedAgvData_ValidData_ReturnsSelectedData()
    {
        var fixture = new AgvDataRepositoryTestFixture();

        List<AgvData> specifications = Enumerable.Range(0, 4).Select(_ => AgvDataGenerator.GenerateAgvData()).ToList();
        await fixture.AddRangeAsync(specifications);

        List<Guid> correlationIds = specifications.Take(2).Select(s => s.CorrelationId).ToList();
        ICollection<AgvData> selectedAgvData = await fixture.Repository.GetSelectedAgvDataAsync(correlationIds);
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

        List<AgvData> knownData = Enumerable.Range(0, 4).Select(_ => AgvDataGenerator.GenerateAgvData()).ToList();
        await fixture.AddRangeAsync(knownData);

        List<Guid> correlationIds = knownData.Select(s => s.CorrelationId).ToList();
        correlationIds.Add("Unknown".ToGuid());
        ICollection<AgvData> actualDataOfAgvs = await fixture.Repository.GetSelectedAgvDataAsync(correlationIds);
        actualDataOfAgvs.ShouldNotBeNull();
        actualDataOfAgvs.Count.ShouldBe(knownData.Count);
        foreach (var actualAgvData in actualDataOfAgvs)
        {
            actualAgvData.ShouldBeEquivalentTo(knownData.Single(s => s.CorrelationId == actualAgvData.CorrelationId));
        }
    }

    [TestMethod]
    public async Task GetLargestAgvDimensionAsync_ValidAgvData_ReturnsMaxWidthMaxLength()
    {
        var fixture = new AgvDataRepositoryTestFixture();

        List<AgvData> specifications = Enumerable.Range(0, 4).Select(_ => AgvDataGenerator.GenerateAgvData()).ToList();
        double expectedMaxWidth = specifications.Max(s => s.Width);
        double expectedMaxLength = specifications.Max(s => s.Length);
        await fixture.AddRangeAsync(specifications);

        (double maxWidth, double maxLength) = await fixture.Repository.GetLargestAgvDimensionAsync();

        maxWidth.ShouldBe(expectedMaxWidth);
        maxLength.ShouldBe(expectedMaxLength);
    }

    [TestMethod]
    public async Task GetLargestAgvDimensionAsync_NoAgvData_ReturnsDefaultValue()
    {
        var fixture = new AgvDataRepositoryTestFixture();

        (double maxWidth, double maxLength) = await fixture.Repository.GetLargestAgvDimensionAsync();

        maxWidth.ShouldBe(0.0);
        maxLength.ShouldBe(0.0);
    }
}