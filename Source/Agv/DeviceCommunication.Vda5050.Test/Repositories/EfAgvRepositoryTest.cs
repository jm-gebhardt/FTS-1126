using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Repositories;

[TestClass]
public class EfAgvRepositoryTest
{
    [TestMethod]
    public async Task AddOrUpdateAsync_ExistingAgvInDatabaseWithEmptyCache_UpdatesStoredAgv()
    {
        var fixture = new AgvRepositoryTestFixture();

        var existingAgv = AgvGenerator.Generate(Guid.NewGuid(), serialNumber: "agv-1");
        existingAgv.InterfaceName = "vda";
        existingAgv.ProtocolMajorVersion = "2";
        await fixture.AddAsync(existingAgv);

        var updatedAgv = AgvGenerator.Generate(existingAgv.CorrelationId, serialNumber: existingAgv.SerialNumber);
        updatedAgv.SeriesName = "updated-series";
        updatedAgv.CurrentOrder = Guid.NewGuid();
        updatedAgv.CurrentFleetManagerActionId = Guid.NewGuid();
        updatedAgv.InterfaceName = "if";
        updatedAgv.ProtocolMajorVersion = "3";

        await fixture.Repository.AddOrUpdateAsync(updatedAgv);

        await using var context = await fixture.ContextFactory.CreateDbContextAsync();
        List<Agv> storedAgvs = await context.Set<Agv>()
            .AsNoTracking()
            .Where(agv => agv.CorrelationId == existingAgv.CorrelationId)
            .ToListAsync();

        storedAgvs.Count.ShouldBe(1);
        storedAgvs.Single().ShouldBeEquivalentTo(updatedAgv);
        fixture.Cache.ConcurrentDict[updatedAgv.CorrelationId].ShouldBeEquivalentTo(updatedAgv);
    }

    [TestMethod]
    public async Task GetAgvAsync_AgvOnlyInDatabase_AddsAgvToCache()
    {
        var fixture = new AgvRepositoryTestFixture();

        var agv = AgvGenerator.Generate(Guid.NewGuid(), serialNumber: "agv-2");
        await fixture.AddAsync(agv);

        var result = await fixture.Repository.GetRequiredAgvAsync(agv.CorrelationId);

        result.ShouldBeEquivalentTo(agv);
        fixture.Cache.ConcurrentDict[agv.CorrelationId].ShouldBeEquivalentTo(agv);
    }
}
