using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
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
}