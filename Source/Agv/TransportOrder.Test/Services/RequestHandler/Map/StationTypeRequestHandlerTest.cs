using Gebhardt.Acc.Common.Domain.Models.Stations;
using Gebhardt.Acc.Common.Messages.Stations.Requests;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Map;

[TestClass]
public class StationTypeRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetStationTypes_ValidData_ReturnsStationTypes()
    {
        var station = StationTypeRequestHandlerTestFixture.GenerateStation(additionalTypes: StationTypeRequestHandlerTestFixture.AdditionalType);
        var fixture = new StationTypeRequestHandlerTestFixture()
            .WithStations(station)
            .Build();

        Response<GotStationTypes> response = await fixture.GetResponseAsync(new GetStationTypes());

        ICollection<StationTypeInfo> stations = response.Message.Stations;
        stations.Count.ShouldBe(1);
        var info = stations.First();
        info.Name.ShouldBe(StationTypeRequestHandlerTestFixture.StationName);
        info.PrimaryType.ShouldBe(StationTypeRequestHandlerTestFixture.PrimaryType);
        info.AdditionalTypes.ShouldBe([StationTypeRequestHandlerTestFixture.AdditionalType]);
    }

    [TestMethod]
    public async Task Consume_GetStationTypes_NoStations_ReturnsEmpty()
    {
        var fixture = new StationTypeRequestHandlerTestFixture()
            .WithStations()
            .Build();

        Response<GotStationTypes> response = await fixture.GetResponseAsync(new GetStationTypes());

        response.Message.Stations.ShouldBeEmpty();
    }
}