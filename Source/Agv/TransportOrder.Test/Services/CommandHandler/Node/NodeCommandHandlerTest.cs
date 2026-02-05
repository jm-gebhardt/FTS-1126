using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.Graphs;
using Gebhardt.Acc.Common.Messages.Stations;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Node;

[TestClass]
public class NodeCommandHandlerTest
{
    private static readonly GraphNode _node = "Charge-0".ToGraphNode();

    [TestMethod]
    public async Task Consume_FreeNode_ValidData_FreeNodeCalled()
    {
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeState(new NodeLock(_node, NodeLockState.Free, false, null, DateTime.UtcNow))
            .WithAnyStation();

        await fixture.TestHarness.Bus.Publish(new FreeNode(_node.Id));

        await fixture.ShouldHaveConsumedAsync<FreeNode>();
        fixture.NodeStateRepository.Verify(x => x.FreeNodeAsync(It.IsAny<Guid>()), Times.Once);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [DataRow(NodeLockState.Occupied)]
    [DataRow(NodeLockState.Free)]
    [TestMethod]
    public async Task Consume_FreeNodeReservedOnly_NotReserved_FreeNodeNotCalled(NodeLockState state)
    {
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeState(new NodeLock(_node, state, false, null, DateTime.UtcNow))
            .WithAnyStation();

        await fixture.TestHarness.Bus.Publish(new FreeNode(_node.Id, true));

        await fixture.ShouldHaveConsumedAsync<FreeNode>();
        fixture.NodeStateRepository.Verify(x => x.FreeNodeAsync(It.IsAny<Guid>()), Times.Never);
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_FreeNodeReservedOnly_ValidData_FreeNodeCalled()
    {
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeState(new NodeLock(_node, NodeLockState.Reserved, false, null, DateTime.UtcNow))
            .WithAnyStation();

        await fixture.TestHarness.Bus.Publish(new FreeNode(_node.Id, true));

        await fixture.ShouldHaveConsumedAsync<FreeNode>();
        fixture.NodeStateRepository.Verify(x => x.FreeNodeAsync(It.IsAny<Guid>()), Times.Once);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    private const string AgvSerialNumber = "agv1";

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_OldLastChargeNodeWasOccupiedByAgv_UnOccupyNode()
    {
        var oldLastNodeState = new NodeLock("old".ToGraphNode(), NodeLockState.Occupied, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var newLastNode = "new".ToGraphNode();
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([oldLastNodeState])
            .WithNodeOccupied(AgvSerialNumber.ToGuid())
            .WithStation(oldLastNodeState.Node.Name)
            .WithNode(new GraphNode(Guid.NewGuid(), "name") { Type = AgvGraph.NodeType.Charge })
            .WithNode(newLastNode)
            .WithQueue(null);

        await fixture.PublishMessageAsync(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old".ToGuid(),
                newLastNode.Id));

        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        fixture.NodeStateRepository.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Once);
    }

    /// <summary>
    /// Test case for chain of park nodes used as agv queue. Current agv passes parking one.
    /// </summary>
    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_OldLastNodeWasOccupiedByDifferentAgv_UnOccupyNodeNotCalled()
    {
        var oldLastNodeState = new NodeLock("old".ToGraphNode(), NodeLockState.Occupied, false, "different".ToGuid(), DateTime.UtcNow);
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([oldLastNodeState])
            .WithNodeOccupied("different".ToGuid())
            .WithQueue(null);

        await fixture.PublishMessageAsync(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old".ToGuid(),
                "new".ToGuid()));

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
                "old".ToGuid(),
                "new".ToGuid()));

        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        fixture.NodeStateRepository.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_NewLastNodeReservedForAgv_NewLastNodeOccupied()
    {
        var newLastNode = "new".ToGraphNode();
        var newLastNodeState = new NodeLock(newLastNode, NodeLockState.Reserved, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([newLastNodeState])
            .WithNodeReserved(newLastNodeState.Node.Name)
            .WithStation(newLastNodeState.Node.Name)
            .WithQueue(null)
            .WithNode(newLastNode);

        await fixture.TestHarness.Bus.Publish(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old".ToGuid(),
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
        var newLastNodeState = new NodeLock("new".ToGraphNode(), NodeLockState.Reserved, false, "different".ToGuid(), DateTime.UtcNow);
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([newLastNodeState])
            .WithNodeReserved(newLastNodeState.Node.Name)
            .WithQueue(null);

        await fixture.TestHarness.Bus.Publish(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old".ToGuid(),
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
                "old".ToGuid(),
                "new".ToGuid()));

        await fixture.ShouldHaveConsumedAsync<UpdateNodeOccupancy>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        fixture.NodeStateRepository.Verify(r => r.OccupyNodeAsync("new".ToGuid(), AgvSerialNumber.ToGuid()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_OldLastNodeWasOccupiedByAgv_NewLastNodeReservedForAgv_UnOccupyLastNode_OccupyNewNode()
    {
        var oldLastNodeState = new NodeLock("old".ToGraphNode(), NodeLockState.Occupied, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var newLastNodeState = new NodeLock("new".ToGraphNode(), NodeLockState.Reserved, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);

        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([oldLastNodeState, newLastNodeState])
            .WithAnyStation()
            .WithNodeOccupied(AgvSerialNumber.ToGuid())
            .WithNode(new GraphNode(Guid.NewGuid(), "name") { Type = AgvGraph.NodeType.Charge })
            .WithQueue(null);

        await fixture.TestHarness.Bus.Publish(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                oldLastNodeState.NodeId,
                newLastNodeState.NodeId));

        await fixture.ShouldHaveConsumedAsync<UpdateNodeOccupancy>();
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        fixture.NodeStateRepository.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Once);
        fixture.NodeStateRepository.Verify(r => r.OccupyNodeAsync("new".ToGuid(), AgvSerialNumber.ToGuid()), Times.Once);
    }

    [TestMethod]
    [DataRow(59, false)]
    [DataRow(61, true)]
    public async Task Consume_UpdateNodeOccupancy_UnusedNodeLockAgvNotInQueue_NodeFreed(int ageInSeconds, bool shouldHaveBeenFreed)
    {
        var now = DateTime.UtcNow;
        var modifiedDate = now.AddSeconds(-ageInSeconds);
        var unusedNodeState = new NodeLock("unused".ToGraphNode(), NodeLockState.Occupied, false, AgvSerialNumber.ToGuid(), modifiedDate);
        var usedNodeState = new NodeLock("usedNode".ToGraphNode(), NodeLockState.Reserved, false, AgvSerialNumber.ToGuid(), modifiedDate);
        var actionGroupToUsedNode = Helper.AgvActionGenerator.GenerateChargeActionGroup(targetNodeName: usedNodeState.Node.Name, assignedAgv: AgvSerialNumber.ToGuid());
        var fixture = new NodeCommandHandlerTestFixture()
            .WithActionGroups([actionGroupToUsedNode])
            .WithNodeStates([unusedNodeState, usedNodeState])
            .WithAnyStation()
            .WithQueue(null);

        fixture.TimeProvider.SetNow(now);

        await fixture.PublishMessageAsync(new UpdateNodeOccupancy(AgvSerialNumber.ToGuid(), "old".ToGuid(), "new".ToGuid()));

        await fixture.ShouldHaveConsumedAsync<UpdateNodeOccupancy>();

        if (shouldHaveBeenFreed)
        {
            await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
            await fixture.ShouldHavePublishedAsync<FreeStationOccupancy>();
        }
        else
        {
            await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
            await fixture.ShouldNotHavePublishedAsync<FreeStationOccupancy>();
        }

        fixture.NodeStateRepository.Verify(r => r.FreeNodeAsync(unusedNodeState.NodeId), shouldHaveBeenFreed ? Times.Once : Times.Never);
        fixture.NodeStateRepository.Verify(r => r.FreeNodeAsync(usedNodeState.NodeId), Times.Never);

        fixture.NodeStateRepository.Verify(r => r.UnOccupyNodeAsync(AgvSerialNumber.ToGuid()), Times.Never);
        fixture.NodeStateRepository.Verify(r => r.OccupyNodeAsync("new".ToGuid(), AgvSerialNumber.ToGuid()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_CurrentActionTargetsOldLastNode_UnOccupyNotCalled()
    {
        var agvId = AgvSerialNumber.ToGuid();
        const string oldNodeId = "charge-5";
        const string newNodeId = "node99";
        var actionGroup = Helper.AgvActionGenerator.GenerateChargeActionGroup(targetNodeName: oldNodeId, assignedAgv: agvId);

        var oldLastNodeState = new NodeLock(oldNodeId.ToGraphNode(), NodeLockState.Occupied, false, agvId, DateTime.UtcNow);
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeStates([oldLastNodeState])
            .WithActionGroups([actionGroup])
            .WithCurrentAgvAction(actionGroup.Actions[0]);

        await fixture.PublishMessageAsync(new UpdateNodeOccupancy(agvId, oldNodeId.ToGuid(), newNodeId.ToGuid()));
        fixture.NodeStateRepository.Verify(r => r.UnOccupyNodeAsync(agvId), Times.Never);
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_SetNodeApproachingProperties_ValidData_NodeUpdated()
    {
        var node = "nodeId".ToGraphNode();

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
        var node = "nodeId".ToGraphNode();

        var fixture = new NodeCommandHandlerTestFixture()
            .WithNode(node);

        await fixture.TestHarness.Bus.Publish(new InitializeNodeApproachingProperties(node.Id, 12, 12, DockingDirection.Back));

        await fixture.ShouldHaveConsumedAsync<InitializeNodeApproachingProperties>();
        fixture.VerifyNodeUpdated();
        await fixture.ShouldHavePublishedAsync<NodeApproachingPropertiesUpdated>();
    }

    [TestMethod]
    public async Task Consume_FreeStationOccupancy_ValidData_FreeNodeCalled()
    {
        var agv = Guid.NewGuid();
        var node = "PS01".ToGraphNode();

        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeState(new NodeLock(node, NodeLockState.Occupied, false, agv, DateTime.UtcNow))
            .WithAnyStation();

        await fixture.PublishMessageAsync(new FreeStationOccupancy(node.Id, agv));

        fixture.NodeStateRepository.Verify(x => x.FreeNodeAsync(It.IsAny<Guid>()), Times.Once);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_FreeStationOccupancy_StationNotReservedOrOccupied_FreeNodeNotCalled()
    {
        var agv = Guid.NewGuid();
        const string nodeName = "APL1";
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeState(null)
            .WithAnyStation();

        await fixture.PublishMessageAsync(new FreeStationOccupancy(nodeName.ToGuid(), agv));

        fixture.NodeStateRepository.Verify(x => x.FreeNodeAsync(It.IsAny<Guid>()), Times.Never);
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_UnblockStation_StationNotReservedOrOccupied_FreeNodeNotCalled()
    {
        var stationId = "APL1".ToGuid();
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeState(null)
            .WithAnyStation();

        await fixture.PublishMessageAsync(new UnblockStation(stationId));

        fixture.NodeStateRepository.Verify(x => x.FreeNodeAsync(It.IsAny<Guid>()), Times.Never);
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_UnblockStation_ValidData_FreeNodeCalled()
    {
        var agv = Guid.NewGuid();
        var node = "PS01".ToGraphNode();
        var station = new TransportOrder.Domain.Models.Station.Station()
        {
            Id = "APL1".ToGuid(),
            NodeLock = new NodeLock(node, NodeLockState.Occupied, false, agv, DateTime.UtcNow)
        };
        var fixture = new NodeCommandHandlerTestFixture()
            .WithStation(station);

        await fixture.PublishMessageAsync(new UnblockStation(station.Id));

        fixture.NodeStateRepository.Verify(x => x.FreeNodeAsync(It.IsAny<Guid>()), Times.Once);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_OccupyStation_ValidData_ForceReserveNodeCalled()
    {
        var agv = Guid.NewGuid();
        var node = "AP01".ToGraphNode();
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeState(null)
            .WithAnyStation()
            .WithNode(node);

        await fixture.PublishMessageAsync(new OccupyStation(node, agv));

        fixture.NodeStateRepository.Verify(x => x.ForceReserveNodeAsync(node.Id, agv), Times.Once);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_StationReserved_ValidData_ForceReserveNodeCalled()
    {
        var agv = Guid.NewGuid();
        var node = "PS01".ToGraphNode();
        var fixture = new NodeCommandHandlerTestFixture()
            .WithNodeState(null)
            .WithAnyStation()
            .WithNode(node);

        await fixture.PublishMessageAsync(new ReserveStation(node, agv));

        fixture.NodeStateRepository.Verify(x => x.ForceReserveNodeAsync(node.Id, agv), Times.Once);
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }
}