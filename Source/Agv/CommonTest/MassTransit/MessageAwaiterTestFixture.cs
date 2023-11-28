using Gebhardt.StoreWare.Agv.Common.MassTransit;
using MassTransit;
using MassTransit.Context;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.MassTransit;

internal class MessageAwaiterTestFixture<TMessage> where TMessage : class
{
    private Func<ConsumeContext<TMessage>, Task>? _messageHandler;

    public MessageAwaiter<DummyMessage> MessageAwaiter { get; }

    public MessageAwaiterTestFixture()
    {
        var messageReceiverMock = new Mock<IMessageReceiver<DummyMessage>>();

        messageReceiverMock.Setup(x => x.StartReceiveAsync(It.IsAny<Guid>(), It.IsAny<Func<ConsumeContext<DummyMessage>, Task>>()))
            .Callback((Guid _, Func<ConsumeContext<TMessage>, Task> handler) => _messageHandler = handler)
            .Returns(Task.CompletedTask);
        MessageAwaiter = new MessageAwaiter<DummyMessage>(messageReceiverMock.Object);
    }

    public Task SimulateMessageAsync(TMessage message)
    {
        _messageHandler.ShouldNotBeNull();
        return _messageHandler.Invoke(new MessageConsumeContext<TMessage>(Mock.Of<ConsumeContext<DummyMessage>>(), message));
    }
}