using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Station;

[TestClass]
public class StationControllerTests
{
    [TestMethod]
    public async Task GetStation_ShouldSendProperCommandToMediator_WhereCorrelationIdIsValid()
    {
        var map = new TransportOrder.Domain.Models.Map.Map("map".ToGuid(), "map", [], 8, 8, 1, 1, 1, DateTime.UtcNow);
        var station = new TransportOrder.Domain.Models.Station.Station("name", AgvGraph.NodeType.Park, []) { MapName = map.MapId };

        StationControllerTestFixture<GetStation, GetStationsRequestHandler> fixture = new StationControllerTestFixture<GetStation, GetStationsRequestHandler>()
            .WithMap(map)
            .WithStation(station)
            .Build();

        var response = await fixture.GetControllerResultAsync(c => c.GetStation(station.Id));

        response.ShouldNotBeNull();

        var harness = fixture.MediatorTestHarness;

        (await harness.Consumed.Any<GetStation>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotStation>()).ShouldBeTrue();

        (await harness.Published.Any<GetStation>()).ShouldBeTrue();
        (await harness.Published.Any<GotStation>()).ShouldBeTrue();
    }
}