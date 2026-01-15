using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;
using Gebhardt.Acc.OrderTranslator.Messages;
using Gebhardt.Acc.OrderTranslator.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.OrderTranslator.Test.Services.CommandHandler;

[TestClass]
public class StationCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_CreateStation_ValidData_StationCreated()
    {
        var fixture = new StationCommandHandlerTestFixture();

        await fixture.PublishMessageAsync(new CreateStation(OrderGenerator.Apl1));

        await fixture.StationRepository.GetStationByName(OrderGenerator.Apl1.Name).ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Consume_CreateStation_StationAlreadyExists_NoDuplicatesCreated()
    {
        var fixture = new StationCommandHandlerTestFixture();

        await fixture.PublishMessageAsync(new CreateStation(OrderGenerator.Apl1));
        await fixture.PublishMessageAsync(new CreateStation(OrderGenerator.Apl1));

        ICollection<Station> allStations = await fixture.StationRepository.GetAll();
        allStations.Count(s => s.Name == OrderGenerator.Apl1.Name).ShouldBe(1);
    }

    [TestMethod]
    public async Task AddOrUpdateAsync_StationExistsWithDifferentConfig_StationUpdated()
    {
        var fixture = new StationCommandHandlerTestFixture();

        var originalStation = StationGenerator.GetStation("TestStation",
            inputLanes: ["OriginalInput"],
            outputLanes: ["OriginalOutput"]);

        await fixture.StationRepository.AddOrUpdateAsync(originalStation);

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

        await fixture.StationRepository.AddOrUpdateAsync(updatedStation);

        var result = await fixture.StationRepository.GetStationByName("TestStation");
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