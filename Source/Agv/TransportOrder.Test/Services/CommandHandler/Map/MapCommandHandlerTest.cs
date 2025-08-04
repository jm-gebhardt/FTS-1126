using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Map;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Map;

[TestClass]
public class MapCommandHandlerTest
{
    [TestMethod]
    public async Task SetBackground_ShouldPersist_AndPublish_ChangedEvent()
    {
        var fixture = new MapRequestTestFixture<SetBackground, MapCommandHandler>();
        var correlationId = fixture.Maps[0].CorrelationId;
        var request = new SetBackground(
            correlationId,
            Resolution: 0.1,
            XOffset: 12,
            YOffset: 24,
            MapBackgroundGenerator.Image1
        );

        Response<Success> response = await fixture.GetResponseAsync<Success>(request);

        response.Message.ShouldBeOfType<Success>();
        fixture.MapRepositoryMock.Verify(r => r.SetCustomBackgroundAsync(It.Is<MapBackground>(m => m.MapId == correlationId && m.Width == 1)), Times.Once);
        await fixture.ShouldHavePublishedAsync<MapChanged>();
    }

    [TestMethod]
    public async Task SetBackground_ShouldThrow_ArgumentException_WhenImageInvalid()
    {
        var correlationId = Guid.NewGuid();
        byte[] invalidBytes = [0, 1];
        var fixture = new MapRequestTestFixture<SetBackground, MapCommandHandler>();

        await MediatorShould.ThrowRequestExceptionAsync<BitmapDecodingException>(async () => await fixture.GetResponseAsync<Success>(new SetBackground(correlationId, 1, 0, 0, invalidBytes)));
    }

    [TestMethod]
    public async Task DeleteBackground_ShouldPublish_ChangedEvent_WhenCustomBackgroundWasDeleted()
    {
        var fixture = new MapRequestTestFixture<DeleteBackground, MapCommandHandler>();
        var mapBackground = fixture.Maps[0];
        fixture.MapRepositoryMock.Setup(r => r.DeleteCustomBackgroundAsync(mapBackground.CorrelationId)).ReturnsAsync(true);

        await fixture.GetResponseAsync<Success>(new DeleteBackground(mapBackground.CorrelationId));

        await fixture.ShouldHavePublishedAsync<MapChanged>();
    }

    [TestMethod]
    public async Task DeleteBackground_ShouldNotPublish_WhenNoBackgroundExisted()
    {
        var mapBackground = MapBackgroundGenerator.Generate();
        MapRequestTestFixture<DeleteBackground, MapCommandHandler> fixture = new MapRequestTestFixture<DeleteBackground, MapCommandHandler>()
            .WithMapBackground(mapBackground);
        fixture.MapRepositoryMock.Setup(r => r.DeleteCustomBackgroundAsync(mapBackground.CorrelationId)).ReturnsAsync(false);

        await fixture.GetResponseAsync<Success>(new DeleteBackground(mapBackground.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<MapChanged>();
    }
}