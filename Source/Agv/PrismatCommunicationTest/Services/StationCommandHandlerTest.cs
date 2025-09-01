using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.Station;
using Gebhardt.Acc.PrismatCommunication.Messages.Events;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Services;

[TestClass]
public class StationCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_AddOrUpdateStationGroup_ValidData_StationsAdded()
    {
        using var fixture = new StationCommandHandlerTestFixture();

        await fixture.TestHarness.Bus.Publish(new AddOrUpdateStationGroup([new Station("source", "target")], Guid.NewGuid()));

        await fixture.ShouldHaveConsumedAsync<AddOrUpdateStationGroup>();

        fixture.VerifyStationGroupWasAdded(Times.Once());
    }

    [TestMethod]
    public async Task Consume_RemoveAgvFromStation_ValidData_PublishesAgvLeftStation()
    {
        var station = new Station("source", "target") {Node = "node"};
        station.AddAgv(Guid.NewGuid());
        station.Occupancy.ShouldBe(1);
        using var fixture = new StationCommandHandlerTestFixture()
            .WithStation(station);

        await fixture.TestHarness.Bus.Publish(new RemoveAgvFromStation(station.Id, station.AgvCorrelationId!.Value));

        await fixture.ShouldHaveConsumedAsync<RemoveAgvFromStation>();
        station.Occupancy.ShouldBe(0);
        await fixture.ShouldHavePublishedAsync<AgvLeftStation>();
    }

    [TestMethod]
    public async Task Consume_RemoveAgvFromStation_StationNotOccupiedByThisAgv_DoesNotPublishAgvLeftStation()
    {
        var agvCorrelationId = Guid.NewGuid();
        var occupiedStation = new Station("source2", "target3") {Node = "station"};
        occupiedStation.AddAgv(Guid.NewGuid());

        using var fixture = new StationCommandHandlerTestFixture()
            .WithStation(occupiedStation);

        await fixture.TestHarness.Bus.Publish(new RemoveAgvFromStation(occupiedStation.Id, agvCorrelationId));

        await fixture.ShouldHaveConsumedAsync<RemoveAgvFromStation>();
        await fixture.ShouldNotHavePublishedAsync<AgvLeftStation>();
    }

    [TestMethod]
    public async Task Consume_AddAgvToStation_ValidData_OccupiesStation()
    {
        var station = new Station("source", "target") {Node = "node"};
        station.Occupancy.ShouldBe(0);
        var agv = Guid.NewGuid();
        using var fixture = new StationCommandHandlerTestFixture()
            .WithStation(station);

        await fixture.TestHarness.Bus.Publish(new AddAgvToStation(station.Id, agv));

        await fixture.ShouldHaveConsumedAsync<AddAgvToStation>();
        station.Occupancy.ShouldBe(1);
        station.AgvCorrelationId.ShouldBe(agv);
    }

    [TestMethod]
    public async Task Consume_AddAgvToStation_UnknownStation_DoesNotOccupiesStation()
    {
        var station = new Station("source", "target") {Node = "node"};
        station.Occupancy.ShouldBe(0);
        var agv = Guid.NewGuid();
        using var fixture = new StationCommandHandlerTestFixture();

        await fixture.TestHarness.Bus.Publish(new AddAgvToStation(station.Id, agv));

        await fixture.ShouldHaveConsumedAsync<AddAgvToStation>();
        station.Occupancy.ShouldBe(0);
        station.AgvCorrelationId.ShouldBeNull();
    }
}