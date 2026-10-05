using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Station;

[TestClass]
public class StationsControllerTests
{
    [TestMethod]
    public async Task GetStation_ShouldSendProperCommandToMediator_WhereCorrelationIdIsValid()
    {
        var map = MapGenerator.Generate();
        var station = new TransportOrder.Domain.Models.Station.Station("name", AgvGraph.NodeType.Park, []);

        var fixture = new StationsControllerTestFixture<GetStations, StationsRequestHandler>()
            .WithMap(map)
            .WithStation(station)
            .Build();

        var response = await fixture.GetControllerResultAsync(c => c.GetStations(CancellationToken.None));

        response.ShouldNotBeNull();

        var harness = fixture.MediatorTestHarness;

        (await harness.Consumed.Any<GetStations>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotStations>()).ShouldBeTrue();

        (await harness.Published.Any<GetStations>()).ShouldBeTrue();
        (await harness.Published.Any<GotStations>()).ShouldBeTrue();
    }
}