using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Text;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Map;
using TransportOrder.Messages.Requests.Map;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers;

[TestClass]
public class MapControllerTest
{
    [TestMethod]
    public async Task GetMaps_ShouldSendProperCommandToMediator_WhereRequestIsNotDefined()
    {
        var expectedAgvGraphs = new List<AgvGraph>
        {
            AgvGraphGenerator.CreateDefaultGraph(Guid.NewGuid()),
            AgvGraphGenerator.CreateDefaultGraph(Guid.NewGuid())
        };

        MapControllerTestFixture<GetMapList, GetMapListRequestHandler> fixture = new MapControllerTestFixture<GetMapList, GetMapListRequestHandler>()
            .WithAgvGraphs(expectedAgvGraphs)
            .UseDefaultServices()
            .Build();
        var mapController = fixture.Controller;
        ActionResult<IEnumerable<MapDto>> response = await mapController.GetMaps();

        var result = response.Result as OkObjectResult;
        var resultValue = result!.Value as IList<MapDto>;

        resultValue.ShouldNotBeEmpty();

        var harness = fixture.TestHarness!;

        (await harness.Consumed.Any<GetMapList>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotMapList>()).ShouldBeTrue();

        (await harness.Published.Any<GetMapList>()).ShouldBeTrue();
        (await harness.Published.Any<GotMapList>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetMap_ShouldSendProperCommandToMediator_WhereCorrelationIdIsValid()
    {
        byte[] imageByteArray = Encoding.UTF8.GetBytes(MapControllerTestFixture<GetMapImage, GetMapImageRequestHandler>.Base64String);
        Guid correlationId = new("7FE9372B-4219-4D09-8885-966EE125A350");

        MapControllerTestFixture<GetMapImage, GetMapImageRequestHandler> fixture = new MapControllerTestFixture<GetMapImage, GetMapImageRequestHandler>()
            .AddMapRegistryMock()
            .Build();

        var mapController = fixture.Controller;
        var response = await mapController.GetMap(correlationId);

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