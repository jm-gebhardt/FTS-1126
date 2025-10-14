using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Stations;
using Gebhardt.Acc.KaeserSidecar.Messages.JamState;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
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

        await fixture.TestHarness.Bus.Publish(new FreeStationOccupancy("WP-8".ToGuid(), agvCorrelationId));

        await fixture.ShouldHaveConsumedAsync<FreeStationOccupancy>();

        var occupancyAfter = await fixture.StationRepository.GetOccupancyByAgvCorrelationId(agvCorrelationId);
        occupancyAfter.ShouldNotBeNull();
        occupancyAfter.StationId.ShouldBe(fixture.Occupancies[0].StationId);
    }

    [TestMethod]
    public async Task Consume_FreeStation_TryToFreeStation_RemovesOccupancy()
    {
        var agvCorrelationId = Guid.NewGuid();

        var fixture = new StationCommandHandlerTestFixture()
            .WithOccupancies((1, agvCorrelationId));

        await fixture.TestHarness.Bus.Publish(new FreeStationOccupancy(fixture.Occupancies[0].StationId, agvCorrelationId));

        await fixture.ShouldHaveConsumedAsync<FreeStationOccupancy>();

        var occupancyAfter = await fixture.StationRepository.GetOccupancyByAgvCorrelationId(agvCorrelationId);
        occupancyAfter.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_UnblockStation_TryToFreeStation_RemovesOccupancy()
    {
        var agvCorrelationId = Guid.NewGuid();

        var fixture = new StationCommandHandlerTestFixture()
            .WithOccupancies((1, agvCorrelationId));

        await fixture.TestHarness.Bus.Publish(new UnblockStation(fixture.Occupancies[0].StationId));

        await fixture.ShouldHaveConsumedAsync<UnblockStation>();

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

        (await fixture.TestHarness.Published.Any<FreeStationOccupancy>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_StationUnlocked_ConveyorStation_PublishesRequestJamState()
    {
        var fixture = new StationCommandHandlerTestFixture();

        string stationId = OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First();

        await fixture.PublishMessageAsync(new StationUnlocked(stationId.ToGraphNode()));

        await fixture.ShouldHavePublishedAsync<RequestJamState>();
    }

    [TestMethod]
    public async Task Consume_StationUnlocked_NotConveyorStation_DoesNotPublishRequestJamState()
    {
        var fixture = new StationCommandHandlerTestFixture();

        await fixture.PublishMessageAsync(new StationUnlocked("a station".ToGraphNode()));

        await fixture.ShouldNotHavePublishedAsync<RequestJamState>();
    }
}