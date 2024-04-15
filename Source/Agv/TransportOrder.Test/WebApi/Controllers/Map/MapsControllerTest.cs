using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.WebApi.Controllers.Map;

[TestClass]
public class MapsControllerTest
{
    [TestMethod]
    public async Task GetMaps_ShouldSendProperCommandToMediator_WhereRequestIsNotDefined()
    {
        MapsControllerTestFixture<GetMapList, GetMapRequestHandler> fixture = new MapsControllerTestFixture<GetMapList, GetMapRequestHandler>()
            .Build();

        await fixture.GetControllerResultAsync(c => c.GetMaps());

        var harness = fixture.MediatorTestHarness;

        (await harness.Consumed.Any<GetMapList>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotMapList>()).ShouldBeTrue();

        (await harness.Published.Any<GetMapList>()).ShouldBeTrue();
        (await harness.Published.Any<GotMapList>()).ShouldBeTrue();
    }
}