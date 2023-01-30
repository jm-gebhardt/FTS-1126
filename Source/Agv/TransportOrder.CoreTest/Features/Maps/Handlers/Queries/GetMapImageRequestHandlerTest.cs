using Gebhardt.StoreWare.Agv.TransportOrder.Core.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using System.Text;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Maps.Handlers.Queries
{
    [TestClass]
    public class GetMapImageRequestHandlerTest
    {
        [TestMethod]
        public async Task GetMap_ShouldReturnByteArrayOfImage_WhenMapIsFound()
        {
            MapRequestTestFixture<GetMapImageRequest, GetMapImageRequestHandler> fixture = new MapRequestTestFixture<GetMapImageRequest, GetMapImageRequestHandler>()
                .AddMapRegistryMock(m =>
                {
                    m.Setup(x => x.GetMap(It.IsAny<Guid>())).Returns(Encoding.UTF8.GetBytes(MapRequestTestFixture<GetMapImageRequest, GetMapImageRequestHandler>.Base64String));
                })
                .Build();

            Response<GetMapImageResult> response = await fixture.GetResponseAsync<GetMapImageResult>(new(new("7FE9372B-4219-4D09-8885-966EE125A350")));
            byte[] byteArrayOfImage = response.Message.Image;

            byteArrayOfImage.ShouldNotBeEmpty();

            var harness = fixture.TestHarness!;

            (await harness.Consumed.Any<GetMapImageRequest>()).ShouldBeTrue();
            (await harness.Consumed.Any<GetMapImageResult>()).ShouldBeTrue();

            (await harness.Published.Any<GetMapImageRequest>()).ShouldBeTrue();
            (await harness.Published.Any<GetMapImageResult>()).ShouldBeTrue();
        }


        [TestMethod]
        public async Task GetMap_ShouldThrowMapNotFoundException_WhenMapIsNotFound()
        {
            MapRequestTestFixture<GetMapImageRequest, GetMapImageRequestHandler> fixture = new MapRequestTestFixture<GetMapImageRequest, GetMapImageRequestHandler>()
                .AddMapRegistryMock(m =>
                {
                    m.Setup(x => x.GetMap(It.IsAny<Guid>())).Returns(() => null);
                })
                .Build();

            var requestException = await Should.ThrowAsync<RequestException>(fixture.GetResponseAsync<GetMapImageResult>(new(new("7FE9372B-4219-4D09-8885-966EE125A351"))));
            requestException.InnerException.ShouldBeOfType<MapNotFoundException>();
        }

    }
}