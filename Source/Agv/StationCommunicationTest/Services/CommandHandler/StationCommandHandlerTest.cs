using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.StationCommunication.Domain.Models;
using Gebhardt.Acc.StationCommunication.Messages.JamState;
using Gebhardt.Acc.StationCommunication.Messages.Stations;
using Moq;

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

    [TestMethod]
    public async Task Consume_ReloadStationConfiguration_InitializesPlcConnectionRepository()
    {
        using var fixture = new StationCommandHandlerTestFixture()
            .WithConfigurationFile(@"Stations:
- Name: ""PS01""
  HasJamState: True
  PlcConnections: [""FROMWESTATION"", ""APLWESTATION""]");

        await fixture.PublishMessageAsync(new ReloadStationConfiguration());

        fixture.RepositoryMock.Verify(
            r => r.Initialize(
                It.Is<ICollection<Station>>(stations =>
                    stations.Count == 1
                    && stations.Single().Name == "PS01"
                    && stations.Single().HasJamState
                    && new HashSet<string>(stations.Single().PlcConnections).SetEquals(new[] { "FROMWESTATION", "APLWESTATION" })),
                It.Is<IEnumerable<string>>(keys => !keys.Any())),
            Times.Once);
    }
}