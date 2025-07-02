using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Messages.Events;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Gebhardt.Acc.Statistics.Test.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        var fixture = new GridPositionAttachedHandlerTestFixture()
            .WithCachedPosition(message.SerialNumber, message.AgvPosition, gridIds);

        await fixture.Handler.Consume(GridPositionAttachedHandlerTestFixture.CreateContext(message));

        fixture.Handler.HandledMessages.ShouldHaveSingleItem();
        fixture.Handler.HandledMessages[0].ShouldBe((message, gridIds));
    }

    [TestMethod]
    public async Task Consume_WithoutCachedPosition_AddsToPendingAndPublishesForcePersist()
    {
        var message = new TestMessage();

        var fixture = new GridPositionAttachedHandlerTestFixture()
            .WithNoCachedPosition();

        await fixture.Handler.Consume(GridPositionAttachedHandlerTestFixture.CreateContext(message));

        fixture.PendingCacheMock.Verify(x => x.AddItem(message.SerialNumber, message.AgvPosition, message), Times.Once);
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

        var fixture = new GridPositionAttachedHandlerTestFixture()
            .WithPendingMessages(message.SerialNumber, message.AgvPosition, [message]);

        await fixture.Handler.OnGridPositionPersisted(new GridPositionPersisted(message.SerialNumber, message.AgvPosition, gridIds));

        fixture.Handler.HandledMessages.ShouldHaveSingleItem();
        fixture.Handler.HandledMessages[0].ShouldBe((message, gridIds));
    }

    [TestMethod]
    public async Task OnGridPositionPersisted_WithMultiplePending_CallsHandlerForAll()
    {
        var msg1 = new TestMessage();
        var msg2 = new TestMessage { SerialNumber = msg1.SerialNumber, AgvPosition = msg1.AgvPosition };
        var gridIds = new List<Guid> { Guid.NewGuid() };

        var fixture = new GridPositionAttachedHandlerTestFixture()
            .WithPendingMessages(msg1.SerialNumber, msg1.AgvPosition, [msg1, msg2]);

        await fixture.Handler.OnGridPositionPersisted(new GridPositionPersisted(msg1.SerialNumber, msg1.AgvPosition, gridIds));

        fixture.Handler.HandledMessages.Count.ShouldBe(2);
        fixture.Handler.HandledMessages.ShouldContain((msg1, gridIds));
        fixture.Handler.HandledMessages.ShouldContain((msg2, gridIds));
    }

    [TestMethod]
    public async Task OnGridPositionPersisted_WithoutPending_DoesNothing()
    {
        var msg = new TestMessage();
        var gridIds = new List<Guid> { Guid.NewGuid() };

        var fixture = new GridPositionAttachedHandlerTestFixture()
            .WithNoPendingMessages();

        await fixture.Handler.OnGridPositionPersisted(new GridPositionPersisted(msg.SerialNumber, msg.AgvPosition, gridIds));

        fixture.Handler.HandledMessages.ShouldBeEmpty();
    }
}