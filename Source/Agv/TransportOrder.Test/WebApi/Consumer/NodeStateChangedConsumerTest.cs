using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class NodeStateChangedConsumerTest
{
    [TestMethod]
    public async Task Consume_NodeStateChanged_ValidData_SendNodeStateChanged()
    {
        var fixture = new ConsumerHubTestFixture<NodeStateChangedConsumer, IMapPublisher>();
        var nodeState = new NodeLock("NodeId", NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(new NodeStateChanged(nodeState));

        (await fixture.ConsumerHarness.Consumed.Any<NodeStateChanged>()).ShouldBeTrue();
        fixture.PublisherMock.Verify(h => h.SendNodeStateChangedAsync(
            nodeState), Times.Once);
    }
}