using Gebhardt.Acc.Common.Messages.Stations.Events;
using Shouldly;

namespace Gebhardt.Acc.StationCommunication.Test.Services.CommandHandler;

[TestClass]
public class StationOnlineStateCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_NoStations_PublishesNothing()
    {
        var fixture = new StationOnlineStateCommandHandlerTestFixture();
        fixture.ConnectionRepositoryMock
            .Setup(r => r.GetAllStationStates())
            .Returns([]);

        await fixture.TestHarness.Bus.Publish(new StationOnlineStatesRequested());
        await fixture.ShouldHaveConsumedAsync<StationOnlineStatesRequested>();

        await fixture.ShouldNotHavePublishedAsync<StationIsOnlineChanged>();
    }

    [TestMethod]
    public async Task Consume_WithStations_PublishesStationIsOnlineChangedForEach()
    {
        var station1 = Guid.NewGuid();
        var station2 = Guid.NewGuid();
        var fixture = new StationOnlineStateCommandHandlerTestFixture();
        fixture.ConnectionRepositoryMock
            .Setup(r => r.GetAllStationStates())
            .Returns([(station1, true), (station2, false)]);

        await fixture.TestHarness.Bus.Publish(new StationOnlineStatesRequested());
        await fixture.ShouldHaveConsumedAsync<StationOnlineStatesRequested>();

        await fixture.ShouldHavePublishedAsync<StationIsOnlineChanged>();
        var published = fixture.GetPublishedMessages<StationIsOnlineChanged>();
        published.Count.ShouldBe(2);
        published.ShouldContain(e => e.StationId == station1 && e.IsOnline);
        published.ShouldContain(e => e.StationId == station2 && !e.IsOnline);
    }
}