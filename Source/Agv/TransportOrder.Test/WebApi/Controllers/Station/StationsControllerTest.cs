using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Station;

[TestClass]
public class StationsControllerTests
{
    [TestMethod]
    public async Task GetStation_ShouldSendProperCommandToMediator_WhereCorrelationIdIsValid()
    {
        var map = new TransportOrder.Domain.Models.Map.Map("map".ToGuid(), "map", [], 8, 8, 1, 1, 1, DateTime.UtcNow);
        var station = new TransportOrder.Domain.Models.Station.Station("name", AgvGraph.NodeType.Park, []) { MapName = map.MapId };

        StationsControllerTestFixture<GetStations, GetStationsRequestHandler> fixture = new StationsControllerTestFixture<GetStations, GetStationsRequestHandler>()
            .WithMap(map)
            .WithStation(station)
            .Build();

        ICollection<StationDto> response = await fixture.GetControllerResultAsync(c => c.GetStations());

        response.ShouldNotBeNull();

        var harness = fixture.MediatorTestHarness;

        (await harness.Consumed.Any<GetStations>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotStations>()).ShouldBeTrue();

        (await harness.Published.Any<GetStations>()).ShouldBeTrue();
        (await harness.Published.Any<GotStations>()).ShouldBeTrue();
    }
}