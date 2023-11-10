using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers.Map;

[TestClass]
public class MapControllerTest
{
    [TestMethod]
    public async Task GetMap_ShouldSendProperCommandToMediator_WhereCorrelationIdIsValid()
    {
        byte[] imageByteArray = Convert.FromBase64String(MapControllerTestFixture<GetMapImage, GetMapImageRequestHandler>.Base64String);
        Guid correlationId = new("7FE9372B-4219-4D09-8885-966EE125A350");

        MapControllerTestFixture<GetMapImage, GetMapImageRequestHandler> fixture = new MapControllerTestFixture<GetMapImage, GetMapImageRequestHandler>()
            .AddMapRepositoryMock()
            .Build();

        var mapController = fixture.Controller;
        var response = await mapController.GetBackgroundImage(correlationId);

        var result = response as FileContentResult;
        byte[] resultValue = result!.FileContents;

        resultValue.ShouldNotBeEmpty();
        resultValue.ShouldBeEquivalentTo(imageByteArray);

        var harness = fixture.TestHarness!;

        (await harness.Consumed.Any<GetMapImage>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotMapImage>()).ShouldBeTrue();

        (await harness.Published.Any<GetMapImage>()).ShouldBeTrue();
        (await harness.Published.Any<GotMapImage>()).ShouldBeTrue();
    }
}