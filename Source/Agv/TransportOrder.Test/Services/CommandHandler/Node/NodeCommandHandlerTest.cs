using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.Graphs;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
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

    [DataRow(NodeLockState.Occupied)]
    [DataRow(NodeLockState.Free)]
    [DataTestMethod]
    public async Task Consume_FreeNodeReservedOnly_NotReserved_FreeNodeNotCalled(NodeLockState state)
    {
        const string nodeId = "Charge-0";
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeState(new NodeLock(nodeId, state, false, null, DateTime.UtcNow))
            .WithAnyStation();

        await fixture.TestHarness.Bus.Publish(new FreeNode(nodeId, true));

        await fixture.ShouldHaveConsumedAsync<FreeNode>();
        fixture.NodeStateRepository.Verify(x => x.FreeNodeAsync(It.IsAny<string>()), Times.Never);
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_FreeNodeReservedOnly_ValidData_FreeNodeCalled()
    {
        const string nodeId = "Charge-0";
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeState(new NodeLock(nodeId, NodeLockState.Reserved, false, null, DateTime.UtcNow))
            .WithAnyStation();

        await fixture.TestHarness.Bus.Publish(new FreeNode(nodeId, true));

        await fixture.ShouldHaveConsumedAsync<FreeNode>();
        fixture.NodeStateRepository.Verify(x => x.FreeNodeAsync(It.IsAny<string>()), Times.Once);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    private const string AgvSerialNumber = "agv1";

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_OldLastChargeNodeWasOccupiedByAgv_UnOccupyNode()
    {
        var oldLastNodeState = new NodeLock("old", NodeLockState.Occupied, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([oldLastNodeState])
            .WithNodeOccupied(AgvSerialNumber.ToGuid())
            .WithStation(oldLastNodeState.NodeId)
            .WithNode(new GraphNode() { Type = AgvGraph.NodeType.Charge })
            .WithQueue(null);

        await fixture.PublishMessageAsync(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
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

        await fixture.PublishMessageAsync(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        fixture.NodeStateRepository.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Never);
        fixture.NodeStateRepository.Verify(r => r.UnOccupyNodeAsync("different".ToGuid()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_OldLastNodeWasNotOccupied_UnOccupyNodeNotCalled()
    {
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([])
            .WithQueue(null);

        await fixture.PublishMessageAsync(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        fixture.NodeStateRepository.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Never);
    }

    /// <summary>
    /// Test case for chain of park nodes used as agv queue. Current agv passes parking one.
    /// </summary>
    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_OldLastNodeNotChargeOrParkNode_UnOccupyNodeNotCalled()
    {
        var oldLastNodeState = new NodeLock("old", NodeLockState.Occupied, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([oldLastNodeState])
            .WithNodeOccupied(AgvSerialNumber.ToGuid())
            .WithStation(oldLastNodeState.NodeId)
            .WithNode(new GraphNode() { Type = AgvGraph.NodeType.Pick })
            .WithQueue(null);

        await fixture.PublishMessageAsync(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old",
                "new"));

        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
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
            .WithNode(new GraphNode(){ Type = AgvGraph.NodeType.Charge })
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

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_CurrentActionTargetsOldLastNode_UnOccupyNotCalled()
    {
        var agvId = AgvSerialNumber.ToGuid();
        const string oldNodeId = "charge-5";
        const string newNodeId = "node99";
        var actionGroup = Helper.AgvActionGenerator.GenerateChargeActionGroup(targetNodeId: oldNodeId, assignedAgv: agvId);

        var oldLastNodeState = new NodeLock(oldNodeId, NodeLockState.Occupied, false, agvId, DateTime.UtcNow);
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([oldLastNodeState])
            .WithActionGroups([actionGroup])
            .WithCurrentAgvAction(actionGroup.Actions[0]);

        await fixture.PublishMessageAsync(new UpdateNodeOccupancy(agvId, oldNodeId, newNodeId));
        fixture.NodeStateRepository.Verify(r => r.UnOccupyNodeAsync(agvId), Times.Never);
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_SetNodeApproachingProperties_ValidData_NodeUpdated()
    {
        var node = new GraphNode("nodeId");

        var fixture = new NodeCommandHandlerTestFixture()
            .WithNode(node);

        await fixture.TestHarness.Bus.Publish(new SetNodeApproachingProperties(Guid.NewGuid(), node.Id, 12, 12, DockingDirection.Back));

        await fixture.ShouldHaveConsumedAsync<SetNodeApproachingProperties>();
        fixture.VerifyNodeUpdated();
        await fixture.ShouldHavePublishedAsync<NodeApproachingPropertiesUpdated>();
    }

    [TestMethod]
    public async Task Consume_InitializeNodeApproachingProperties_ValidData_NodeUpdated()
    {
        var node = new GraphNode("nodeId");

        var fixture = new NodeCommandHandlerTestFixture()
            .WithNode(node);

        await fixture.TestHarness.Bus.Publish(new InitializeNodeApproachingProperties(node.Id, 12, 12, DockingDirection.Back));

        await fixture.ShouldHaveConsumedAsync<InitializeNodeApproachingProperties>();
        fixture.VerifyNodeUpdated();
        await fixture.ShouldHavePublishedAsync<NodeApproachingPropertiesUpdated>();
    }

    [TestMethod]
    public async Task Consume_FreeStation_ValidData_FreeNodeCalled()
    {
        var agv = Guid.NewGuid();
        const string nodeId = "PS01";
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeState(new NodeLock(nodeId, NodeLockState.Occupied, false, agv, DateTime.UtcNow))
            .WithAnyStation();

        await fixture.PublishMessageAsync<FreeStation, string>(new FreeStation(nodeId, agv));

        fixture.NodeStateRepository.Verify(x => x.FreeNodeAsync(It.IsAny<string>()), Times.Once);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_FreeStation_StationNotReservedOrOccupied_FreeNodeNotCalled()
    {
        var agv = Guid.NewGuid();
        const string nodeId = "APL1";
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeState(null)
            .WithAnyStation();

        await fixture.PublishMessageAsync<FreeStation, string>(new FreeStation(nodeId, agv));

        fixture.NodeStateRepository.Verify(x => x.FreeNodeAsync(It.IsAny<string>()), Times.Never);
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_OccupyStation_ValidData_ForceReserveNodeCalled()
    {
        var agv = Guid.NewGuid();
        const string nodeId = "AP01";
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeState(null)
            .WithAnyStation();

        await fixture.PublishMessageAsync<OccupyStation, string>(new OccupyStation(nodeId, agv));

        fixture.NodeStateRepository.Verify(x => x.ForceReserveNodeAsync(nodeId, agv), Times.Once);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_StationReserved_ValidData_ForceReserveNodeCalled()
    {
        var agv = Guid.NewGuid();
        const string nodeId = "PS01";
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeState(null)
            .WithAnyStation();

        await fixture.PublishMessageAsync<StationReserved, string>(new StationReserved(nodeId, agv));

        fixture.NodeStateRepository.Verify(x => x.ForceReserveNodeAsync(nodeId, agv), Times.Once);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }
}