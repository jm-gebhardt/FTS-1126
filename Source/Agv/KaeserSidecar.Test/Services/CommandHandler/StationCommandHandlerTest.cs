using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.KaeserSidecar.Messages.JamState;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.CommandHandler;

[TestClass]
public class StationCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_FreeStation_TryToFreeNoStation_DoesNotRemoveOccupancy()
    {
        var agvCorrelationId = Guid.NewGuid();

        var fixture = new StationCommandHandlerTestFixture()
            .WithOccupancies((1, agvCorrelationId));

        await fixture.TestHarness.Bus.Publish(new FreeStation("WP-8", agvCorrelationId));

        await fixture.ShouldHaveConsumedAsync<FreeStation>();

        var occupancyAfter = await fixture.StationRepository.GetOccupancyByAgvCorrelationId(agvCorrelationId);
        occupancyAfter.ShouldNotBeNull();
        occupancyAfter.Station.ShouldBe(fixture.Occupancies[0].Station);
    }

    [TestMethod]
    public async Task Consume_FreeStation_TryToFreeStation_RemovesOccupancy()
    {
        var agvCorrelationId = Guid.NewGuid();

        var fixture = new StationCommandHandlerTestFixture()
            .WithOccupancies((1, agvCorrelationId));

        await fixture.TestHarness.Bus.Publish(new FreeStation(fixture.Occupancies[0].Station, agvCorrelationId));

        await fixture.ShouldHaveConsumedAsync<FreeStation>();

        var occupancyAfter = await fixture.StationRepository.GetOccupancyByAgvCorrelationId(agvCorrelationId);
        occupancyAfter.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_ForceAgvLeftStationReceived_OccupiedStations_EmitsFreeStations()
    {
        var agvId = Guid.NewGuid();

        var fixture = new StationCommandHandlerTestFixture()
            .WithOccupancies((2, agvId));

        await fixture.TestHarness.Bus.Publish(new SolveResourceErrorsReceived(agvId));

        await fixture.ShouldHaveConsumedAsync<SolveResourceErrorsReceived>();

        (await fixture.TestHarness.Published.Any<FreeStation>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_StationUnlocked_ConveyorStation_PublishesRequestJamState()
    {
        var fixture = new StationCommandHandlerTestFixture();

        string station = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();

        await fixture.PublishMessageAsync<StationUnlocked, string>(new StationUnlocked(station));

        await fixture.ShouldHavePublishedAsync<RequestJamState>();
    }

    [TestMethod]
    public async Task Consume_StationUnlocked_NotConveyorStation_DoesNotPublishRequestJamState()
    {
        var fixture = new StationCommandHandlerTestFixture();

        await fixture.PublishMessageAsync<StationUnlocked, string>(new StationUnlocked("a station"));

        await fixture.ShouldNotHavePublishedAsync<RequestJamState>();
    }
}