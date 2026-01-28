using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;
using Gebhardt.Acc.OrderTranslator.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.OrderTranslator.Test.Repositories;

[TestClass]
public class StationRepositoryTest
{
    [TestMethod]
    public async Task AddOrUpdateAsync_StationExistsWithDifferentConfig_StationUpdated()
    {
        var fixture = new StationRepositoryTestFixture();

        var originalStation = StationGenerator.GetStation("TestStation",
            inputLanes: ["OriginalInput"],
            outputLanes: ["OriginalOutput"]);

        await fixture.Repository.AddOrUpdateAsync(originalStation);

        var updatedStation = StationGenerator.GetStation("TestStation",
            needsCargoReporting: true,
            needsPalletReporting: true,
            pickDoppelspielMode: DoppelspielMode.Always,
            dropDoppelspielMode: DoppelspielMode.Order,
            inputLanes: ["UpdatedInput"],
            outputLanes: ["UpdatedOutput"],
            pickRouting: [new Routing("Station1", "Node1")],
            dropRouting: [new Routing("Station2", "Node2")],
            stationGroup: ["GroupStation"]);

        await fixture.Repository.AddOrUpdateAsync(updatedStation);

        var result = await fixture.Repository.GetStationByName("TestStation");
        result.ShouldNotBeNull();
        result.NeedsCargoReporting.ShouldBeTrue();
        result.NeedsPalletReporting.ShouldBeTrue();
        result.PickDoppelspielMode.ShouldBe(DoppelspielMode.Always);
        result.DropDoppelspielMode.ShouldBe(DoppelspielMode.Order);
        result.InputLanes.ShouldContain("UpdatedInput");
        result.OutputLanes.ShouldContain("UpdatedOutput");
        result.PickRouting.Count.ShouldBe(1);
        result.DropRouting.Count.ShouldBe(1);
        result.StationGroup.ShouldContain("GroupStation");
    }
}