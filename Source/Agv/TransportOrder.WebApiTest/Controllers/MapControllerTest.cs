using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers
{
    [TestClass]
    public class MapControllerTest
    {
        [TestMethod]
        public async Task GetMaps_ShouldSendProperCommandToMediator_WhereRequestIsNotDefined()
        {
            var maps = new List<MapDto>()
            {
                new(Guid.NewGuid(),"1",0,10,10,new []{ Guid.NewGuid() })
            };
            var mediatorMock = new Mock<IMediator>();
            mediatorMock.Setup(m => m.Send(It.IsAny<GetMapListRequest>(), CancellationToken.None)).ReturnsAsync(maps);

            var mapController = new MapController(mediatorMock.Object);
            ActionResult<IEnumerable<MapDto>> response = await mapController.GetMaps();

            var result = response.Result as OkObjectResult;
            var resultValue = result!.Value as IList<MapDto>;

            resultValue.ShouldNotBeEmpty();
        }

        [TestMethod]
        public async Task GetMap_ShouldSendProperCommandToMediator_WhereCorrelationIdIsValid()
        {
            byte[] imageByteArray = Convert.FromBase64String("bGlnaHQgd29yay4=");
            Guid correlationId = new("1647B1E4-79B3-4404-A274-572C41678A42");

            var mediatorMock = new Mock<IMediator>();
            mediatorMock.Setup(m => m.Send(It.IsAny<GetMapImageRequest>(), CancellationToken.None)).ReturnsAsync(imageByteArray);

            var mapController = new MapController(mediatorMock.Object);
            var response = await mapController.GetMap(correlationId);

            var result = response as FileContentResult;
            byte[] resultValue = result!.FileContents;

            resultValue.ShouldNotBeEmpty();
            resultValue.ShouldBeEquivalentTo(imageByteArray);
        }
    }
}
