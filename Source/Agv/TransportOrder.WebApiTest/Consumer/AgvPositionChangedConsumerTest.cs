using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using TransportOrder.Messages.Events.Agv;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Consumer;

[TestClass]
public class AgvPositionChangedConsumerTest
{
    private const string AgvId = "agv1";

    [TestMethod]
    public async Task Receiving_AgvPositionChanged_SendsAgvPositionToAllClients()
    {
        var fixture = new ConsumerHubTestFixture<AgvPositionChangedConsumer, IAgvHubContext>();
        await fixture.TestHarness!.Bus.Publish(
            new AgvPositionChanged(
                new AgvPosition(false, 0, 0, 0, 0),
                null,
                AgvId,
                AgvId.ToGuid()
            ));

        (await fixture.TestHarness.Published.Any<AgvPositionChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvPositionChanged>()).ShouldBeTrue();

        fixture.MockHub.Verify(h => h.SendAgvPositionToAllClients(It.Is<AgvPositionStatus>(o => o.AgvId == AgvId)), Times.Once);
    }

    [DataRow("oldLastNodeId", "newLastNodeId")]
    [DataRow("oldLastNodeId", null)]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_UnOccupyCalled(string oldLastNodeId, string newLastNodeId)
    {
        var fixture = new ConsumerHubTestFixture<AgvPositionChangedConsumer, IAgvHubContext>();
        await fixture.TestHarness!.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvId.ToGuid(),
                oldLastNodeId,
                newLastNodeId));
        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();

        fixture.MockNodeStateRegistry.Verify(r => r.UnOccupyNodeAsync(AgvId.ToGuid()), Times.Once);
    }

    [DataRow(null, null)]
    [DataRow(null, "newLastNodeId")]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_UnOccupyNotCalled(string oldLastNodeId, string newLastNodeId)
    {
        var fixture = new ConsumerHubTestFixture<AgvPositionChangedConsumer, IAgvHubContext>();
        await fixture.TestHarness!.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvId.ToGuid(),
                oldLastNodeId,
                newLastNodeId));
        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();

        fixture.MockNodeStateRegistry.Verify(r => r.UnOccupyNodeAsync(AgvId.ToGuid()), Times.Never);
    }

    [DataRow(null, "newLastNodeId")]
    [DataRow("oldLastNodeId", "newLastNodeId")]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_NodeReserved_OccupyNodeAsyncCalled(string oldLastNodeId, string newLastNodeId)
    {
        var fixture = new ConsumerHubTestFixture<AgvPositionChangedConsumer, IAgvHubContext>();
        fixture.MockNodeStateRegistry.Setup(x => x.IsNodeReservedAsync(It.IsAny<string>())).Returns(Task.FromResult(true));
        await fixture.TestHarness!.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvId.ToGuid(),
                oldLastNodeId,
                newLastNodeId));
        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();

        fixture.MockNodeStateRegistry.Verify(r => r.OccupyNodeAsync(newLastNodeId, AgvId.ToGuid()), Times.Once);
    }

    [DataRow(null, "newLastNodeId")]
    [DataRow("oldLastNodeId", "newLastNodeId")]
    [TestMethod]
    public async Task Receiving_AgvLastNodeIdChanged_NodeNotReserved_OccupyNodeAsyncNotCalled(string oldLastNodeId, string newLastNodeId)
    {
        var fixture = new ConsumerHubTestFixture<AgvPositionChangedConsumer, IAgvHubContext>();
        fixture.MockNodeStateRegistry.Setup(x => x.IsNodeReservedAsync(It.IsAny<string>())).Returns(Task.FromResult(false));
        await fixture.TestHarness!.Bus.Publish(
            new AgvLastNodeIdChanged(
                AgvId.ToGuid(),
                oldLastNodeId,
                newLastNodeId));
        (await fixture.TestHarness.Published.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();
        (await fixture.ConsumerHarness!.Consumed.Any<AgvLastNodeIdChanged>()).ShouldBeTrue();

        fixture.MockNodeStateRegistry.Verify(r => r.OccupyNodeAsync(newLastNodeId, AgvId.ToGuid()), Times.Never);
    }
}