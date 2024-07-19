using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Map;

[TestClass]
public class GetMapRequestHandlerTest
{
    [TestMethod]
    public async Task GetMap_ShouldReturnMap_WhenMapExists()
    {
        var fixture = new MapRequestTestFixture<GetMap, GetMapRequestHandler>();

        Response<GotMap> response = await fixture.GetResponseAsync<GotMap>(new GetMap(fixture.Maps[0].CorrelationId));

        fixture.MapRepositoryMock.Verify(m => m.GetMapAsync(fixture.Maps[0].CorrelationId), Times.Once);
        response.Message.Map.CorrelationId.ShouldBe(fixture.Maps[0].CorrelationId);
    }

    [TestMethod]
    public async Task GetMaps_ShouldReturnListOfMaps_WhereMapRepositoryIsContained()
    {
        var fixture = new MapRequestTestFixture<GetMapList, GetMapRequestHandler>();

        Response<GotMapList> response = await fixture.GetResponseAsync<GotMapList>(new GetMapList());
        List<TransportOrder.Messages.Requests.Map.Map> maps = response.Message.Maps;

        maps.ShouldNotBeEmpty();

        var harness = fixture.MediatorTestHarness;

        (await harness.Consumed.Any<GetMapList>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotMapList>()).ShouldBeTrue();

        (await harness.Published.Any<GetMapList>()).ShouldBeTrue();
        (await harness.Published.Any<GotMapList>()).ShouldBeTrue();
    }
}