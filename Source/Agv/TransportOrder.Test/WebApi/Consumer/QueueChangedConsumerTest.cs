using AutoFixture;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.TransportOrder.Messages.Events.Queue;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Queue;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class QueueChangedConsumerTest
{
    [TestMethod]
    public async Task Consume_QueueChanged_ValidData_SendsQueueChangedAsync()
    {
        var fixture = new ConsumerHubTestFixture<QueueChangedConsumer, IQueuePublisher>();

        var autoFixture = new Fixture();
        var data = autoFixture
            .Build<Queue>()
            .With(q => q.QueueNodes,
                autoFixture
                    .Build<QueueNode>()
                    .Without(n => n.Node)
                    .CreateMany(2)
                    .ToList())
            .Create();

        await fixture.PublishMessageAsync(new QueueChanged(data));

        fixture.PublisherMock.Verify(h => h.SendQueueChangedAsync(It.Is<Queue>(q => q.CorrelationId == data.CorrelationId)), Times.Once);
    }
}