using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Text;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers
{
    [TestClass]
    public class MapControllerTest
    {
        [TestMethod]
        public async Task GetMaps_ShouldSendProperCommandToMediator_WhereRequestIsNotDefined()
        {
            MapControllerTestFixture<GetMapListRequest, GetMapListRequestHandler> fixture = new MapControllerTestFixture<GetMapListRequest, GetMapListRequestHandler>()
                .UseDefaultServices()
                .Build();

            var mapController = new MapController(fixture.GetRequestClient(), null!);
            ActionResult<IEnumerable<MapDto>> response = await mapController.GetMaps();

            var result = response.Result as OkObjectResult;
            var resultValue = result!.Value as IList<MapDto>;

            resultValue.ShouldNotBeEmpty();

            var harness = fixture.TestHarness!;

            (await harness.Consumed.Any<GetMapListRequest>()).ShouldBeTrue();
            (await harness.Consumed.Any<GetMapListResult>()).ShouldBeTrue();

            (await harness.Published.Any<GetMapListRequest>()).ShouldBeTrue();
            (await harness.Published.Any<GetMapListResult>()).ShouldBeTrue();
        }

        [TestMethod]
        public async Task GetMap_ShouldSendProperCommandToMediator_WhereCorrelationIdIsValid()
        {
            byte[] imageByteArray = Encoding.UTF8.GetBytes(MapControllerTestFixture<GetMapImageRequest, GetMapImageRequestHandler>.Base64String);
            Guid correlationId = new("7FE9372B-4219-4D09-8885-966EE125A350");

            MapControllerTestFixture<GetMapImageRequest, GetMapImageRequestHandler> fixture = new MapControllerTestFixture<GetMapImageRequest, GetMapImageRequestHandler>()
                .AddMapRegistryMock()
                .Build();

            var mapController = new MapController(null!, fixture.GetRequestClient());
            var response = await mapController.GetMap(correlationId);

            var result = response as FileContentResult;
            byte[] resultValue = result!.FileContents;

            resultValue.ShouldNotBeEmpty();
            resultValue.ShouldBeEquivalentTo(imageByteArray);

            var harness = fixture.TestHarness!;

            (await harness.Consumed.Any<GetMapImageRequest>()).ShouldBeTrue();
            (await harness.Consumed.Any<GetMapImageResult>()).ShouldBeTrue();

            (await harness.Published.Any<GetMapImageRequest>()).ShouldBeTrue();
            (await harness.Published.Any<GetMapImageResult>()).ShouldBeTrue();
        }
    }
}
