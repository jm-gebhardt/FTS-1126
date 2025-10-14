using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Messages.Events;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Gebhardt.Acc.Statistics.Test.Models;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Services.EventHandler;

[TestClass]
public class GridPositionAttachedHandlerBaseTest
{
    [TestMethod]
    public async Task Consume_WithCachedPosition_CallsHandleWithGridIds()
    {
        var message = new TestMessage();
        var gridIds = new List<Guid> { Guid.NewGuid() };

        var fixture = new GridPositionAttachedTestHandlerTestFixture()
            .WithCachedPosition(message.SerialNumber, message.AgvPosition, message.MessageCreatedDate, gridIds)
            .ShouldAct(true);

        await fixture.Handler.Consume(GridPositionAttachedTestHandlerTestFixture.CreateContext(message));

        fixture.Handler.HandledMessages.ShouldHaveSingleItem();
        fixture.Handler.HandledMessages[0].ShouldBe((message, gridIds));
    }

    [TestMethod]
    public async Task Consume_WithoutCachedPosition_AddsToPendingAndPublishesForcePersist()
    {
        var message = new TestMessage();

        var fixture = new GridPositionAttachedTestHandlerTestFixture()
            .WithNoCachedPosition()
            .ShouldAct(true);

        await fixture.Handler.Consume(GridPositionAttachedTestHandlerTestFixture.CreateContext(message));

        fixture.PendingCacheMock.Verify(x => x.AddItem(message.SerialNumber, message.AgvPosition, message.MessageCreatedDate, message), Times.Once);
        fixture.PublishEndpointMock.Verify(x => x.Publish(
            It.Is<ForcePersistAgvPosition>(m =>
                m.SerialNumber == message.SerialNumber &&
                m.AgvPosition.Equals(message.AgvPosition) &&
                m.MessageCreatedDate == message.MessageCreatedDate),
            CancellationToken.None), Times.Once);
    }

    [TestMethod]
    public async Task OnGridPositionPersisted_WithSinglePending_CallsHandler()
    {
        var message = new TestMessage();
        var gridIds = new List<Guid> { Guid.NewGuid() };

        var fixture = new GridPositionAttachedTestHandlerTestFixture()
            .WithPendingMessages(message.SerialNumber, message.AgvPosition, message.MessageCreatedDate, [message])
            .ShouldAct(true);

        await fixture.Handler.OnGridPositionPersisted(new GridPositionPersisted(message.SerialNumber, message.AgvPosition, message.MessageCreatedDate, gridIds));

        fixture.Handler.HandledMessages.ShouldHaveSingleItem();
        fixture.Handler.HandledMessages[0].ShouldBe((message, gridIds));
    }

    [TestMethod]
    public async Task OnGridPositionPersisted_WithMultiplePending_CallsHandlerForAll()
    {
        var message1 = new TestMessage();
        var message2 = new TestMessage { SerialNumber = message1.SerialNumber, AgvPosition = message1.AgvPosition };
        var gridIds = new List<Guid> { Guid.NewGuid() };

        var fixture = new GridPositionAttachedTestHandlerTestFixture()
            .WithPendingMessages(message1.SerialNumber, message1.AgvPosition, message1.MessageCreatedDate, [message1, message2])
            .ShouldAct(true);

        await fixture.Handler.OnGridPositionPersisted(new GridPositionPersisted(message1.SerialNumber, message1.AgvPosition, message1.MessageCreatedDate, gridIds));

        fixture.Handler.HandledMessages.Count.ShouldBe(2);
        fixture.Handler.HandledMessages.ShouldContain((message1, gridIds));
        fixture.Handler.HandledMessages.ShouldContain((message2, gridIds));
    }

    [TestMethod]
    public async Task OnGridPositionPersisted_WithoutPending_DoesNothing()
    {
        var message = new TestMessage();
        var gridIds = new List<Guid> { Guid.NewGuid() };

        var fixture = new GridPositionAttachedTestHandlerTestFixture()
            .WithNoPendingMessages()
            .ShouldAct(true);

        await fixture.Handler.OnGridPositionPersisted(new GridPositionPersisted(message.SerialNumber, message.AgvPosition, message.MessageCreatedDate, gridIds));

        fixture.Handler.HandledMessages.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task HandleWithGridIds_FilterReturnsFalse_DoesNotPublishAddWifiMetrics()
    {
        var message = new TestMessage();

        var fixture = new GridPositionAttachedTestHandlerTestFixture()
            .WithNoPendingMessages()
            .ShouldAct(false);

        await fixture.Handler.Consume(GridPositionAttachedTestHandlerTestFixture.CreateContext(message));

        fixture.Handler.HandledMessages.ShouldBeEmpty();
    }
}