using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.MassTransit;

[TestClass]
public class MessageAwaiterTest
{
    [TestMethod]
    public async Task WaitAsync_MessageReceived_ReturnsMessage()
    {
        var correlationId = Guid.NewGuid();
        var message = new DummyMessage(correlationId);
        var fixture = new MessageAwaiterTestFixture<DummyMessage>();

        var receivedMessage = await fixture.MessageAwaiter.WaitAsync(correlationId,
            _ => true,
            new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token,
            async () => { await fixture.SimulateMessageAsync(message); });

        receivedMessage.ShouldBe(message);
    }

    [TestMethod]
    public async Task WaitAsync_MessageSuccessAtSecondTryWhenCheckContinuouslyUntilSuccess_ReturnsMessage()
    {
        var correlationId = Guid.NewGuid();
        var message = new DummyMessage(correlationId);
        var fixture = new MessageAwaiterTestFixture<DummyMessage>();

        var receivedMessage = await fixture.MessageAwaiter.WaitAsync(correlationId,
            m => m.CorrelationId == message.CorrelationId,
            new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token,
            async () =>
            {
                await fixture.SimulateMessageAsync(new DummyMessage(Guid.NewGuid()));
                await fixture.SimulateMessageAsync(message);
            },
            true);

        receivedMessage.ShouldBe(message);
    }

    [TestMethod]
    public async Task WaitAsync_MessageNoSuccessAndNoCheckContinuouslyUntilSuccess_ThrowsException()
    {
        var correlationId = Guid.NewGuid();
        var message = new DummyMessage(correlationId);
        var fixture = new MessageAwaiterTestFixture<DummyMessage>();

        await Should.ThrowAsync<WaitForMessageFailedException<DummyMessage>>(async () => await fixture.MessageAwaiter.WaitAsync(correlationId,
            _ => false,
            new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token,
            async () => { await fixture.SimulateMessageAsync(message); }));
    }

    [TestMethod]
    public async Task WaitAsync_ReceiveIsStarted_ExecutesAfterListeningTask()
    {
        var fixture = new MessageAwaiterTestFixture<DummyMessage>();
        bool wasExecuted = false;
        await fixture.MessageAwaiter.WaitAsync(Guid.NewGuid(),
            _ => true,
            new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token,
            async () =>
            {
                wasExecuted = true;
                await fixture.SimulateMessageAsync(null!);
            });

        wasExecuted.ShouldBeTrue();
    }

    [TestMethod]
    public async Task WaitAsync_NoMessage_ThrowsException()
    {
        var fixture = new MessageAwaiterTestFixture<DummyMessage>();

        await Should.ThrowAsync<WaitForMessageFailedException<DummyMessage>>(async () => await fixture.MessageAwaiter.WaitAsync(Guid.NewGuid(),
            _ => false,
            new CancellationTokenSource(TimeSpan.FromMilliseconds(1)).Token,
            () => Task.CompletedTask));
    }
}