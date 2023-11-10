using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Map;

[TestClass]
public class GetMapListRequestHandlerTest
{
    [TestMethod]
    public async Task GetMaps_ShouldReturnListOfMaps_WhereMapRepositoryIsContained()
    {
        MapRequestTestFixture<GetMapList, GetMapRequestHandler> fixture = new MapRequestTestFixture<GetMapList, GetMapRequestHandler>()
            .UseDefaultServices()
            .Build();

        Response<GotMapList> response = await fixture.GetResponseAsync<GotMapList>(new GetMapList());
        List<Messages.Requests.Map.Map> maps = response.Message.Maps;

        maps.ShouldNotBeEmpty();

        var harness = fixture.TestHarness!;

        (await harness.Consumed.Any<GetMapList>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotMapList>()).ShouldBeTrue();

        (await harness.Published.Any<GetMapList>()).ShouldBeTrue();
        (await harness.Published.Any<GotMapList>()).ShouldBeTrue();
    }
}