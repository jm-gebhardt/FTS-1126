using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Node;

[TestClass]
public class NodeCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_FreeNode_ValidData_FreeNodeCalled()
    {
        const string nodeId = "Charge-0";
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeState(new NodeLock(nodeId, NodeLockState.Free, false, null, DateTime.UtcNow))
            .WithAnyStation();

        await fixture.TestHarness.Bus.Publish(new FreeNode(nodeId));

        await fixture.ShouldHaveConsumedAsync<FreeNode>();
        fixture.NodeStateRepository.Verify(x => x.FreeNodeAsync(It.IsAny<string>()), Times.Once);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    private const string AgvSerialNumber = "agv1";

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_OldLastNodeWasOccupiedByAgv_UnOccupyNode()
    {
        var oldLastNodeState = new NodeLock("old", NodeLockState.Occupied, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([oldLastNodeState])
            .WithNodeOccupied(AgvSerialNumber.ToGuid())
            .WithStation(oldLastNodeState.NodeId)
            .WithQueue(null);

        await fixture.TestHarness.Bus.Publish(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldHaveConsumedAsync<UpdateNodeOccupancy>();
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<TryReachQueueHead>();
        fixture.NodeStateRepository.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Once);
    }

    /// <summary>
    /// Test case for chain of park nodes used as agv queue. Current agv passes parking one.
    /// </summary>
    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_OldLastNodeWasOccupiedByDifferentAgv_UnOccupyNodeNotCalled()
    {
        var oldLastNodeState = new NodeLock("old", NodeLockState.Occupied, false, "different".ToGuid(), DateTime.UtcNow);
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([oldLastNodeState])
            .WithNodeOccupied("different".ToGuid())
            .WithQueue(null);

        await fixture.TestHarness.Bus.Publish(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldHaveConsumedAsync<UpdateNodeOccupancy>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<TryReachQueueHead>();
        fixture.NodeStateRepository.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Never);
        fixture.NodeStateRepository.Verify(r => r.UnOccupyNodeAsync("different".ToGuid()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_OldLastNodeWasNotOccupied_UnOccupyNodeNotCalled()
    {
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([])
            .WithQueue(null);

        await fixture.TestHarness.Bus.Publish(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldHaveConsumedAsync<UpdateNodeOccupancy>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<TryReachQueueHead>();
        fixture.NodeStateRepository.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_NewLastNodeReservedForAgv_NewLastNodeOccupied()
    {
        var newLastNodeState = new NodeLock("new", NodeLockState.Reserved, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([newLastNodeState])
            .WithNodeReserved(newLastNodeState.NodeId)
            .WithStation(newLastNodeState.NodeId)
            .WithQueue(null);

        await fixture.TestHarness.Bus.Publish(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old",
                newLastNodeState.NodeId));

        await fixture.ShouldHaveConsumedAsync<UpdateNodeOccupancy>();
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldHavePublishedAsync<TryReachQueueHead>();
        fixture.NodeStateRepository.Verify(r => r.OccupyNodeAsync(newLastNodeState.NodeId, AgvSerialNumber.ToGuid()), Times.Once);
    }

    /// <summary>
    /// Test case for chain of park nodes used as agv queue. Current agv passes parking node reserved for different agv.
    /// </summary>
    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_NewLastNodeReservedForDifferentAgv_NewLastNodeNotOccupied()
    {
        var newLastNodeState = new NodeLock("new", NodeLockState.Reserved, false, "different".ToGuid(), DateTime.UtcNow);
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([newLastNodeState])
            .WithNodeReserved(newLastNodeState.NodeId)
            .WithQueue(null);

        await fixture.TestHarness.Bus.Publish(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old",
                newLastNodeState.NodeId));

        await fixture.ShouldHaveConsumedAsync<UpdateNodeOccupancy>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        await fixture.ShouldNotHavePublishedAsync<TryReachQueueHead>();
        fixture.NodeStateRepository.Verify(r => r.OccupyNodeAsync(newLastNodeState.NodeId, "different".ToGuid()), Times.Never);
        fixture.NodeStateRepository.Verify(r => r.OccupyNodeAsync(newLastNodeState.NodeId, AgvSerialNumber.ToGuid()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_NewLastNodeNotReserved_NewLastNodeNotOccupied()
    {
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([])
            .WithQueue(null);

        await fixture.TestHarness.Bus.Publish(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldHaveConsumedAsync<UpdateNodeOccupancy>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        fixture.NodeStateRepository.Verify(r => r.OccupyNodeAsync("new", AgvSerialNumber.ToGuid()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_OldLastNodeWasOccupiedByAgv_NewLastNodeReservedForAgv_UnOccupyLastNode_OccupyNewNode()
    {
        var oldLastNodeState = new NodeLock("old", NodeLockState.Occupied, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var newLastNodeState = new NodeLock("new", NodeLockState.Reserved, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);

        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([oldLastNodeState, newLastNodeState])
            .WithAnyStation()
            .WithNodeOccupied(AgvSerialNumber.ToGuid())
            .WithQueue(null);

        await fixture.TestHarness.Bus.Publish(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldHaveConsumedAsync<UpdateNodeOccupancy>();
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        fixture.NodeStateRepository.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Once);
        fixture.NodeStateRepository.Verify(r => r.OccupyNodeAsync("new", AgvSerialNumber.ToGuid()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_UnusedNodeLockAgvNotInQueue_NodeFreed()
    {
        var unusedNodeState = new NodeLock("unused", NodeLockState.Occupied, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var usedNodeState = new NodeLock("usedNode", NodeLockState.Reserved, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var actionGroupToUsedNode = Helper.AgvActionGenerator.GenerateChargeActionGroup(targetNodeId: usedNodeState.NodeId, assignedAgv: AgvSerialNumber.ToGuid());
        var fixture = new NodeCommandHandlerTestFixture()
            .WithActionGroups([actionGroupToUsedNode])
            .WithNodeStates([unusedNodeState, usedNodeState])
            .WithAnyStation()
            .WithQueue(null);

        await fixture.TestHarness.Bus.Publish(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldHaveConsumedAsync<UpdateNodeOccupancy>();
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();

        fixture.NodeStateRepository.Verify(r => r.FreeNodeAsync(unusedNodeState.NodeId), Times.Once);
        fixture.NodeStateRepository.Verify(r => r.FreeNodeAsync(usedNodeState.NodeId), Times.Never);

        fixture.NodeStateRepository.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Never);
        fixture.NodeStateRepository.Verify(r => r.OccupyNodeAsync("new", AgvSerialNumber.ToGuid()), Times.Never);
    }
}