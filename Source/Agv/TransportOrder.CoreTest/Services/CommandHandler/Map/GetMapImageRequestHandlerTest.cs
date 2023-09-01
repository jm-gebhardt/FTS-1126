using System.Text;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using TransportOrder.Domain.Exceptions;
using TransportOrder.Messages.Requests.Map;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Map;

[TestClass]
public class GetMapImageRequestHandlerTest
{
    [TestMethod]
    public async Task GetMap_ShouldReturnByteArrayOfImage_WhenMapIsFound()
    {
        MapRequestTestFixture<GetMapImage, GetMapImageRequestHandler> fixture = new MapRequestTestFixture<GetMapImage, GetMapImageRequestHandler>()
            .AddMapRegistryMock(m =>
            {
                m.Setup(x => x.GetMap(It.IsAny<Guid>())).Returns(Encoding.UTF8.GetBytes(MapRequestTestFixture<GetMapImage, GetMapImageRequestHandler>.Base64String));
            })
            .Build();

        Response<GotMapImage> response = await fixture.GetResponseAsync<GotMapImage>(new(new("7FE9372B-4219-4D09-8885-966EE125A350")));
        byte[] byteArrayOfImage = response.Message.Image;

        byteArrayOfImage.ShouldNotBeEmpty();

        var harness = fixture.TestHarness!;

        (await harness.Consumed.Any<GetMapImage>()).ShouldBeTrue();
        (await harness.Consumed.Any<GotMapImage>()).ShouldBeTrue();

        (await harness.Published.Any<GetMapImage>()).ShouldBeTrue();
        (await harness.Published.Any<GotMapImage>()).ShouldBeTrue();
    }


    [TestMethod]
    public async Task GetMap_ShouldThrowMapNotFoundException_WhenMapIsNotFound()
    {
        MapRequestTestFixture<GetMapImage, GetMapImageRequestHandler> fixture = new MapRequestTestFixture<GetMapImage, GetMapImageRequestHandler>()
            .AddMapRegistryMock(m =>
            {
                m.Setup(x => x.GetMap(It.IsAny<Guid>())).Returns(() => null);
            })
            .Build();

        var requestException = await Should.ThrowAsync<RequestException>(fixture.GetResponseAsync<GotMapImage>(new(new("7FE9372B-4219-4D09-8885-966EE125A351"))));
        requestException.InnerException.ShouldBeOfType<MapNotFoundException>();
    }

}