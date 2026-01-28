using Gebhardt.Acc.Common.Messages.Stations.Events;
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

        await fixture.ShouldHavePublishedAsync<StationConfigured>();
    }

    [TestMethod]
    public async Task Consume_CreateStation_StationAlreadyExists_NoDuplicatesCreated()
    {
        var fixture = new StationCommandHandlerTestFixture();

        await fixture.PublishMessageAsync(new CreateStation(OrderGenerator.Apl1));
        await fixture.PublishMessageAsync(new CreateStation(OrderGenerator.Apl1));

        ICollection<Station> allStations = await fixture.StationRepository.GetAll();
        allStations.Count(s => s.Name == OrderGenerator.Apl1.Name).ShouldBe(1);
        await fixture.ShouldHavePublishedAsync<StationConfigured>();
    }
}