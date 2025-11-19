using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using GotQueue = Gebhardt.Acc.TransportOrder.Messages.Requests.Queue.GotQueue;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Queue;

public class QueueRequestHandlerTestFixture<TMessage> : MediatorTestFixture<TMessage, QueueRequestHandler, MockConsumer>
    where TMessage : class
{
    private readonly Mock<IQueueRepository> _queueRepository = new();
    private readonly Mock<IQueueManager> _queueManager = new();

    // ReSharper disable once StaticMemberInGenericType
    private static TransportOrder.Domain.Models.Queue.Queue _queue = null!;

    private class MockAddQueueConsumer : IConsumer<AddQueue>
    {
        public async Task Consume(ConsumeContext<AddQueue> context)
        {
            await context.RespondAsync(new GotQueue(_queue));
        }
    }

    private MockAddQueueConsumer ConsumerMock { get; } = new();

    public new QueueRequestHandlerTestFixture<TMessage> Build()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _queueRepository.Object);
            s.AddTransient(_ => _queueManager.Object);
            s.AddTransient(_ => ConsumerMock);
        });
        AddConsumers<MockAddQueueConsumer>();
        base.Build();
        return this;
    }

    public QueueRequestHandlerTestFixture<TMessage> WithQueue(TransportOrder.Domain.Models.Queue.Queue queue)
    {
        _queueRepository.Setup(x => x.GetQueues()).ReturnsAsync([queue]);
        _queueRepository.Setup(x => x.GetQueueById(It.IsAny<Guid>())).ReturnsAsync(queue);
        _queueRepository.Setup(x => x.RemoveQueue(It.IsAny<Guid>())).Verifiable();
        _queue = queue;
        return this;
    }

    public QueueRequestHandlerTestFixture<TMessage> WithAgvIsInQueue(Guid agvCorrelationId)
    {
        _queueManager.Setup(x => x.IsAgvInQueue(agvCorrelationId)).ReturnsAsync(true);
        return this;
    }
}