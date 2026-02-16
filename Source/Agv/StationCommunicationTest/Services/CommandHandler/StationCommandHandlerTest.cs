using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.StationCommunication.Messages.JamState;
using Gebhardt.Acc.StationCommunication.Messages.Stations;

namespace Gebhardt.Acc.StationCommunication.Test.Services.CommandHandler;

[TestClass]
public class StationCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_ReloadStationConfiguration_StationWithJamState_PublishesStationConfiguredAndRequestJamState()
    {
        using var fixture = new StationCommandHandlerTestFixture()
            .WithConfigurationFile(@"Stations:
- Name: ""PS01""
  HasJamState: True");

        await fixture.TestHarness.Bus.Publish(new ReloadStationConfiguration());
        await fixture.ShouldHaveConsumedAsync<ReloadStationConfiguration>();
        await fixture.ShouldHavePublishedAsync<StationConfigured>();
        await fixture.ShouldHavePublishedAsync<RequestJamState>();
    }

    [TestMethod]
    public async Task Consume_ReloadStationConfiguration_StationWithoutJamState_PublishesStationConfiguredOnly()
    {
        using var fixture = new StationCommandHandlerTestFixture()
            .WithConfigurationFile(@"Stations:
- Name: ""PS01""
  HasJamState: False");

        await fixture.TestHarness.Bus.Publish(new ReloadStationConfiguration());
        await fixture.ShouldHaveConsumedAsync<ReloadStationConfiguration>();
        await fixture.ShouldHavePublishedAsync<StationConfigured>();
        await fixture.ShouldNotHavePublishedAsync<RequestJamState>();
    }
}