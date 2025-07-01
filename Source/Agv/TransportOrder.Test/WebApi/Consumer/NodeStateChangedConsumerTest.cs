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
        var fixture = new ConsumerHubTestFixture<NodeChangedConsumer, IMapPublisher>();
        
        const string nodeId = "NodeId";
        var dateModified = DateTime.UtcNow;
        var nodeState = new NodeLock(nodeId, NodeLockState.Reserved, false, Guid.NewGuid(), dateModified);
        var stationId = Guid.NewGuid();

        await fixture.TestHarness.Bus.Publish(new NodeStateChanged(nodeState, stationId, null, null, null));

        (await fixture.ConsumerHarness.Consumed.Any<NodeStateChanged>()).ShouldBeTrue();
        fixture.PublisherMock.Verify(h => h.SendNodeStateChangedAsync(nodeState, stationId, null, null, null),Times.Once);
    }
}