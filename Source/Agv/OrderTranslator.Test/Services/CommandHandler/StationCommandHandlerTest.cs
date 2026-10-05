using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.OrderTranslator.Messages;
using Gebhardt.Acc.OrderTranslator.Messages.Stations;
using Gebhardt.Acc.OrderTranslator.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.OrderTranslator.Test.Services.CommandHandler;

[TestClass]
public class StationCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_CreateOrUpdateStation_ValidData_StationCreated()
    {
        var fixture = new StationCommandHandlerTestFixture();

        await fixture.PublishMessageAsync(new CreateOrUpdateStation(OrderGenerator.Apl1));

        await fixture.StationRepository.GetStationByName(OrderGenerator.Apl1.Name).ShouldNotBeNull();

        await fixture.ShouldHavePublishedAsync<StationConfigured>();
    }

    [TestMethod]
    public async Task Consume_CreateOrUpdateStation_StationAlreadyExists_NoDuplicatesCreated()
    {
        var fixture = new StationCommandHandlerTestFixture();

        await fixture.PublishMessageAsync(new CreateOrUpdateStation(OrderGenerator.Apl1));
        await fixture.PublishMessageAsync(new CreateOrUpdateStation(OrderGenerator.Apl1));

        var allStations = await fixture.StationRepository.GetAll();
        allStations.Count(s => s.Name == OrderGenerator.Apl1.Name).ShouldBe(1);
        await fixture.ShouldHavePublishedAsync<StationConfigured>();
    }

    [TestMethod]
    public async Task Consume_ReloadStationConfiguration_ValidData_ConfigurationReloadedAndPublished()
    {
        using var fixture = new StationCommandHandlerTestFixture()
            .WithConfigurationFile(@"Stations:
- Name: PS01
  StationGroup:
  InputLanes: [1020]
  OutputLanes: [1040]");

        await fixture.PublishMessageAsync(new ReloadStationConfiguration());

        await fixture.ShouldHavePublishedAsync<StationConfigurationReceived>();
        await fixture.ShouldHavePublishedAsync<CreateOrUpdateStation>();
    }
}