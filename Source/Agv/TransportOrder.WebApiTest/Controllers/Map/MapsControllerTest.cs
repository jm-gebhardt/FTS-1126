using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Map;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers.Map;

[TestClass]
public class MapsControllerTest
{
    [TestMethod]
    public async Task GetMaps_ShouldSendProperCommandToMediator_WhereRequestIsNotDefined()
    {
        var expectedAgvGraphs = new List<AgvGraph>
        {
            AgvGraphGenerator.CreateDefaultGraph(Guid.NewGuid()),
            AgvGraphGenerator.CreateDefaultGraph(Guid.NewGuid())
        };

        MapsControllerTestFixture<GetMapList, GetMapRequestHandler> fixture = new MapsControllerTestFixture<GetMapList, GetMapRequestHandler>()
            .WithAgvGraphs(expectedAgvGraphs)
            .UseDefaultServices()
            .Build();
        var mapController = fixture.Controller;
        ActionResult<IEnumerable<MapMetaDataDto>> response = await mapController.GetMaps();

        var result = response.Result as OkObjectResult;
        var resultValue = result!.Value as IEnumerable<MapMetaDataDto>;

        resultValue.ShouldNotBeEmpty();

        var harness = fixture.TestHarness;

        (await harness.Consumed.Any<GetMapList>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotMapList>()).ShouldBeTrue();

        (await harness.Published.Any<GetMapList>()).ShouldBeTrue();
        (await harness.Published.Any<GotMapList>()).ShouldBeTrue();
    }
}