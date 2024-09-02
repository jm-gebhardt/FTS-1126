using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Map;

[TestClass]
public class GetStationsRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetMapStations_ValidData_ReturnsStations()
    {
        var map = new TransportOrder.Domain.Models.Map.Map("map".ToGuid(), "map", [], 8, 8, 1, 1, 1, DateTime.UtcNow);
        var station = new Station("name", AgvGraph.NodeType.Park, []) { MapName = map.MapId };
        GetStationsRequestHandlerTestFixture<GetMapStations, GetStationsRequestHandler> fixture = new GetStationsRequestHandlerTestFixture<GetMapStations, GetStationsRequestHandler>()
            .WithMap(map)
            .WithStation(station)
            .Build();

        Response<GotMapStations> response = await fixture.GetResponseAsync<GotMapStations>(new GetMapStations(map.CorrelationId));

        ICollection<Station> gotStations = response.Message.Stations;
        gotStations.ShouldNotBeNull();
        gotStations.Count.ShouldBe(1);
        gotStations.First().ShouldBe(station);
    }

    [TestMethod]
    public async Task Consume_GetStations_ValidData_ReturnsStations()
    {
        var station = new Station("name", AgvGraph.NodeType.Park, []);
        GetStationsRequestHandlerTestFixture<GetStations, GetStationsRequestHandler> fixture = new GetStationsRequestHandlerTestFixture<GetStations, GetStationsRequestHandler>()
            .WithStation(station)
            .Build();

        Response<GotStations> response = await fixture.GetResponseAsync<GotStations>(new GetStations(Guid.NewGuid()));

        ICollection<Station> gotStations = response.Message.Stations;
        gotStations.ShouldNotBeNull();
        gotStations.Count.ShouldBe(1);
        gotStations.First().ShouldBe(station);
    }

    [TestMethod]
    public async Task Consume_GetStation_ValidData_ReturnsStations()
    {
        var station = new Station("name", AgvGraph.NodeType.Park, []);
        GetStationsRequestHandlerTestFixture<GetStation, GetStationsRequestHandler> fixture = new GetStationsRequestHandlerTestFixture<GetStation, GetStationsRequestHandler>()
            .WithStation(station)
            .Build();

        Response<GotStation> response = await fixture.GetResponseAsync<GotStation>(new GetStation(station.Id));

        response.Message.Station.ShouldBe(station);
    }
}