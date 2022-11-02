using Gebhardt.StoreWare.Agv.TransportOrder.Common.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using System.Text;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Features.Maps.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Features.Maps.Handlers.Queries;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Features.Maps.Handlers.Queries
{
    [TestClass]
    public class GetMapImageRequestHandlerTest
    {
        [TestMethod]
        public async Task GetMap_ShouldReturnByteArrayOfImage_WhenMapIsFound()
        {
            var mapRegistryMock = new Mock<IMapRegistry>();
            mapRegistryMock.Setup(x => x.GetMap(It.IsAny<Guid>())).Returns(Encoding.UTF8.GetBytes(MapRequestHandlerTestFixture.Base64String));

            var mapRequest = new GetMapImageRequest(new("7FE9372B-4219-4D09-8885-966EE125A350"));
            var mapCommandHandler = new GetMapImageRequestHandler(mapRegistryMock.Object);

            byte[] byteArrayOfImage = await mapCommandHandler.Handle(mapRequest, CancellationToken.None);

            byteArrayOfImage.ShouldNotBeEmpty();
        }

        [TestMethod]
        [ExpectedException(typeof(MapNotFoundException))]
        public async Task GetMap_ShouldThrowMapNotFoundException_WhenMapIsNotFound()
        {
            var mapRegistryMock = new Mock<IMapRegistry>();
            mapRegistryMock.Setup(x => x.GetMap(It.IsAny<Guid>())).Returns(() => null);

            var mapRequest = new GetMapImageRequest(new("7FE9372B-4219-4D09-8885-966EE125A351"));
            var mapCommandHandler = new GetMapImageRequestHandler(mapRegistryMock.Object);

            await mapCommandHandler.Handle(mapRequest, CancellationToken.None);
        }

    }
}
