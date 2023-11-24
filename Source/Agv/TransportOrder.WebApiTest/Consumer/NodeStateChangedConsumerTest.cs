using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

[TestClass]
public class NodeStateChangedConsumerTest
{
    [TestMethod]
    public async Task Consume_NodeStateChanged_ValidData_SendNodeStateChanged()
    {
        var fixture = new ConsumerHubTestFixture<NodeStateChangedConsumer, IMapPublisher>();
        var nodeState = new NodeLock("NodeId", NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow);

        await fixture.TestHarness!.Bus.Publish(new NodeStateChanged(nodeState));

        (await fixture.ConsumerHarness!.Consumed.Any<NodeStateChanged>()).ShouldBeTrue();
        fixture.PublisherMock.Verify(h => h.SendNodeStateChangedAsync(
            nodeState), Times.Once);
    }
}