using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Gebhardt.Acc.TransportOrder.WebApi.Consumer;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Consumer;

[TestClass]
public class NodeStateChangedConsumerTest
{
    [TestMethod]
    public async Task Consume_NodeStateChanged_ValidData_SendNodeStateChanged()
    {
        var fixture = new ConsumerHubTestFixture<NodeChangedConsumer, IMapPublisher>();

        const string nodeName = "NodeId";
        var dateModified = DateTime.UtcNow;
        var nodeState = new NodeLock(nodeName.ToGraphNode(), NodeLockState.Reserved, false, Guid.NewGuid(), dateModified);
        var stationId = Guid.NewGuid();

        await fixture.TestHarness.Bus.Publish(new NodeStateChanged(nodeState, nodeName, stationId, null, null, null));

        await fixture.ShouldHaveConsumedAsync<NodeStateChanged>();
        fixture.PublisherMock.Verify(h => h.SendNodeStateChangedAsync(nodeState, nodeName, stationId, null, null, null), Times.Once);
    }

    [TestMethod]
    public async Task Consume_JamStateChanged_ValidData_SendJamStateUpdate()
    {
        var stationId = Guid.NewGuid();
        const bool hasPalletJam = true;
        var fixture = new ConsumerHubTestFixture<NodeChangedConsumer, IMapPublisher>();

        await fixture.PublishMessageAsync(new JamStateChanged(stationId, hasPalletJam, DateTime.UtcNow.AddMinutes(1), false, DateTime.UtcNow));

        fixture.PublisherMock.Verify(h => h.SendJamStateUpdate(stationId, hasPalletJam), Times.Once);
    }
}