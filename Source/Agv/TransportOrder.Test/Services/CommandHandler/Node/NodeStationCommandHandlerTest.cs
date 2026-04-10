using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.Graphs;
using Gebhardt.Acc.Common.Messages.Stations;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Node;

[TestClass]
public class NodeStationCommandHandlerTest
{
    private static readonly GraphNode _node = "Charge-0".ToGraphNode();

    [TestMethod]
    public async Task Consume_FreeNode_ValidData_FreeNodeCalled()
    {
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLock(new NodeLock(_node, NodeLockState.Free, false, null, DateTime.UtcNow))
            .WithAnyStation();

        await fixture.TestHarness.Bus.Publish(new FreeNode(_node.Id));

        await fixture.ShouldHaveConsumedAsync<FreeNode>();
        fixture.VerifyNodeLockUpdated(Times.Once());
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [DataRow(NodeLockState.Occupied)]
    [DataRow(NodeLockState.Free)]
    [TestMethod]
    public async Task Consume_FreeNodeReservedOnly_NotReserved_FreeNodeNotCalled(NodeLockState state)
    {
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLock(new NodeLock(_node, state, false, null, DateTime.UtcNow))
            .WithAnyStation();

        await fixture.TestHarness.Bus.Publish(new FreeNode(_node.Id, true));

        await fixture.ShouldHaveConsumedAsync<FreeNode>();
        fixture.VerifyNodeLockUpdated(Times.Never());
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_FreeNodeReservedOnly_ValidData_FreeNodeCalled()
    {
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLock(new NodeLock(_node, NodeLockState.Reserved, false, null, DateTime.UtcNow))
            .WithAnyStation();

        await fixture.TestHarness.Bus.Publish(new FreeNode(_node.Id, true));

        await fixture.ShouldHaveConsumedAsync<FreeNode>();
        fixture.VerifyNodeLockUpdated(Times.Once());
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    private const string AgvSerialNumber = "agv1";

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_OldLastChargeNodeWasOccupiedByAgv_UnOccupyNode()
    {
        var oldLastNodeState = new NodeLock("old".ToGraphNode(), NodeLockState.Occupied, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var newLastNode = "new".ToGraphNode();
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLocks([oldLastNodeState])
            .WithNodeOccupied()
            .WithStation("old")
            .WithNode(new GraphNode(Guid.NewGuid(), "name") { Type = AgvGraph.NodeType.Charge })
            .WithNode(newLastNode)
            .WithQueue(null);

        await fixture.PublishMessageAsync(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old".ToGuid(),
                newLastNode.Id));

        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        fixture.VerifyNodeLockUpdated(Times.Once());
    }

    /// <summary>
    /// Test case for chain of park nodes used as agv queue. Current agv passes parking one.
    /// </summary>
    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_OldLastNodeWasOccupiedByDifferentAgv_UnOccupyNodeNotCalled()
    {
        var oldLastNodeState = new NodeLock("old".ToGraphNode(), NodeLockState.Occupied, false, "different".ToGuid(), DateTime.UtcNow);
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLocks([oldLastNodeState])
            .WithNodeOccupied()
            .WithQueue(null);

        await fixture.PublishMessageAsync(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old".ToGuid(),
                "new".ToGuid()));

        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        fixture.VerifyNodeLockUpdated(Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_OldLastNodeWasNotOccupied_UnOccupyNodeNotCalled()
    {
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLocks([])
            .WithQueue(null);

        await fixture.PublishMessageAsync(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old".ToGuid(),
                "new".ToGuid()));

        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        fixture.VerifyNodeLockUpdated(Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_NewLastNodeReservedForAgv_NewLastNodeOccupied()
    {
        var newLastNode = "new".ToGraphNode();
        var newLastNodeState = new NodeLock(newLastNode, NodeLockState.Reserved, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLocks([newLastNodeState])
            .WithNodeReserved()
            .WithStation("new")
            .WithQueue(null)
            .WithNode(newLastNode);

        await fixture.TestHarness.Bus.Publish(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old".ToGuid(),
                newLastNodeState.NodeId));

        await fixture.ShouldHaveConsumedAsync<UpdateNodeOccupancy>();
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        fixture.VerifyNodeLockUpdated(Times.Once());
    }

    /// <summary>
    /// Test case for chain of park nodes used as agv queue. Current agv passes parking node reserved for different agv.
    /// </summary>
    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_NewLastNodeReservedForDifferentAgv_NewLastNodeNotOccupied()
    {
        var newLastNodeState = new NodeLock("new".ToGraphNode(), NodeLockState.Reserved, false, "different".ToGuid(), DateTime.UtcNow);
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLocks([newLastNodeState])
            .WithNodeReserved()
            .WithQueue(null);

        await fixture.TestHarness.Bus.Publish(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old".ToGuid(),
                newLastNodeState.NodeId));

        await fixture.ShouldHaveConsumedAsync<UpdateNodeOccupancy>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        fixture.VerifyNodeLockUpdated(Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_NewLastNodeNotReserved_NewLastNodeNotOccupied()
    {
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLocks([])
            .WithQueue(null);

        await fixture.TestHarness.Bus.Publish(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                "old".ToGuid(),
                "new".ToGuid()));

        await fixture.ShouldHaveConsumedAsync<UpdateNodeOccupancy>();
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
        fixture.VerifyNodeLockUpdated(Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_OldLastNodeWasOccupiedByAgv_NewLastNodeReservedForAgv_UnOccupyLastNode_OccupyNewNode()
    {
        var oldLastNodeState = new NodeLock("old".ToGraphNode(), NodeLockState.Occupied, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);
        var newLastNodeState = new NodeLock("new".ToGraphNode(), NodeLockState.Reserved, false, AgvSerialNumber.ToGuid(), DateTime.UtcNow);

        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLocks([oldLastNodeState, newLastNodeState])
            .WithAnyStation()
            .WithNodeOccupied()
            .WithNode(new GraphNode(Guid.NewGuid(), "name") { Type = AgvGraph.NodeType.Charge })
            .WithQueue(null);

        await fixture.TestHarness.Bus.Publish(
            new UpdateNodeOccupancy(
                AgvSerialNumber.ToGuid(),
                oldLastNodeState.NodeId,
                newLastNodeState.NodeId));

        await fixture.ShouldHaveConsumedAsync<UpdateNodeOccupancy>();
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        fixture.VerifyNodeLockUpdated(Times.Exactly(2));
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
        var actionGroupToUsedNode = Helper.AgvActionGenerator.GenerateChargeActionGroup(targetNodeName: "usedNode", assignedAgv: AgvSerialNumber.ToGuid());
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithActionGroups([actionGroupToUsedNode])
            .WithNodeLocks([unusedNodeState, usedNodeState])
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

        fixture.VerifyNodeLockUpdated(shouldHaveBeenFreed ? Times.Once() : Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateNodeOccupancy_CurrentActionTargetsOldLastNode_UnOccupyNotCalled()
    {
        var agvId = AgvSerialNumber.ToGuid();
        const string oldNodeId = "charge-5";
        const string newNodeId = "node99";
        var actionGroup = Helper.AgvActionGenerator.GenerateChargeActionGroup(targetNodeName: oldNodeId, assignedAgv: agvId);

        var oldLastNodeState = new NodeLock(oldNodeId.ToGraphNode(), NodeLockState.Occupied, false, agvId, DateTime.UtcNow);
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLocks([oldLastNodeState])
            .WithActionGroups([actionGroup])
            .WithCurrentAgvAction(actionGroup.Actions[0]);

        await fixture.PublishMessageAsync(new UpdateNodeOccupancy(agvId, oldNodeId.ToGuid(), newNodeId.ToGuid()));
        fixture.VerifyNodeLockUpdated(Times.Never());
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_SetNodeApproachingProperties_ValidData_NodeUpdated()
    {
        var node = "nodeId".ToGraphNode();

        var fixture = new NodeStationCommandHandlerTestFixture()
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

        var fixture = new NodeStationCommandHandlerTestFixture()
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

        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLock(new NodeLock(node, NodeLockState.Occupied, false, agv, DateTime.UtcNow))
            .WithAnyStation();

        await fixture.PublishMessageAsync(new FreeStationOccupancy(node.Id, agv));

        fixture.VerifyNodeLockUpdated(Times.Once());
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_FreeStationOccupancy_StationNotReservedOrOccupied_FreeNodeNotCalled()
    {
        var agv = Guid.NewGuid();
        const string nodeName = "APL1";
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLock(null)
            .WithAnyStation();

        await fixture.PublishMessageAsync(new FreeStationOccupancy(nodeName.ToGuid(), agv));

        fixture.VerifyNodeLockUpdated(Times.Never());
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_UnblockStation_StationNotReservedOrOccupied_FreeNodeNotCalled()
    {
        var stationId = "APL1".ToGuid();
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLock(null)
            .WithAnyStation();

        await fixture.PublishMessageAsync(new UnblockStation(stationId));

        fixture.VerifyNodeLockUpdated(Times.Never());
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_UnblockStation_ValidData_FreeNodeCalled()
    {
        var agv = Guid.NewGuid();
        var node = "PS01".ToGraphNode();
        var station = new Station()
        {
            Id = "APL1".ToGuid(),
            NodeLock = new NodeLock(node, NodeLockState.Occupied, false, agv, DateTime.UtcNow)
        };
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithStation(station)
            .WithAnyStation();

        await fixture.PublishMessageAsync(new UnblockStation(station.Id));

        fixture.VerifyNodeLockUpdated(Times.Once());
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_OccupyStation_ValidData_ForceOccupyNodeCalled()
    {
        var agv = Guid.NewGuid();
        var node = "AP01".ToGraphNode();
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLock(null)
            .WithAnyStation()
            .WithNode(node);

        await fixture.PublishMessageAsync(new OccupyStation(node, agv));

        fixture.VerifyNodeLockUpdated(Times.Once());
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_StationReserved_ValidData_ForceReserveNodeCalled()
    {
        var agv = Guid.NewGuid();
        var node = "PS01".ToGraphNode();
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithNodeLock(null)
            .WithAnyStation()
            .WithNode(node);

        await fixture.PublishMessageAsync(new ReserveStation(node, agv));

        fixture.VerifyNodeLockUpdated(Times.Once());
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_LockStation_ValidData_NodeStateUpdated()
    {
        var node = "PD-0".ToGraphNode();
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);
        const string user = "User";
        const string reason = "Reason";
        const string description = "Description";

        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithStation(node.Name)
            .WithNode(node);

        await fixture.TestHarness.Bus.Publish(new LockStation(node.Id, user, reason, description, LockedOrderHandlingType.AllowFinish));

        await fixture.ShouldHaveConsumedAsync<LockStation>();
        fixture.VerifyStationUpdated(Times.Once());
        fixture.VerifyNodeLockUpdated(Times.Once());
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        var nodeStateChanged = fixture.TestHarness.Published.Select<NodeStateChanged>().First().Context.Message;
        nodeStateChanged.LockDescription.ShouldBe(description);
        nodeStateChanged.LockReason.ShouldBe(reason);
        nodeStateChanged.ModifyingUser.ShouldBe(user);
        nodeStateChanged.StationId.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Consume_UnlockStation_ValidData_NodeStateUpdated()
    {
        var node = "PD-0".ToGraphNode();
        node.Lock = new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow);
        const string user = "User";
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithStation(node.Name)
            .WithNode(node);

        await fixture.TestHarness.Bus.Publish(new UnlockStation(node.Id, user));

        await fixture.ShouldHaveConsumedAsync<UnlockStation>();
        fixture.VerifyStationUpdated(Times.Once());
        fixture.VerifyNodeLockUpdated(Times.Once());
        await fixture.ShouldHavePublishedAsync<NodeStateChanged>();
        var nodeStateChanged = fixture.TestHarness.Published.Select<NodeStateChanged>().First().Context.Message;
        nodeStateChanged.LockDescription.ShouldBeNull();
        nodeStateChanged.LockReason.ShouldBeNull();
        nodeStateChanged.ModifyingUser.ShouldBe(user);
        nodeStateChanged.StationId.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Consume_UnlockStation_NotUnlockableNode_NodeStateChangedNotCalled()
    {
        const string nodeName = "PD-0";
        var node = nodeName.ToGraphNode();
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);

        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithStation(nodeName)
            .WithNode(node);

        var nodeId = nodeName.ToGuid();
        await fixture.TestHarness.Bus.Publish(new UnlockStation(nodeId, "User"));

        await fixture.ShouldHaveConsumedAsync<UnlockStation>();
        fixture.VerifyStationUpdated(Times.Never());
        fixture.VerifyNodeLockUpdated(Times.Never());
        await fixture.ShouldNotHavePublishedAsync<NodeStateChanged>();
    }

    [TestMethod]
    public async Task Consume_UpdateStationJamState_ValidData_PublishesJamStateChanged()
    {
        const string station = "station";
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithStation(station);

        await fixture.PublishMessageAsync(new UpdateStationJamState(station.ToGuid(), true));

        await fixture.ShouldHavePublishedAsync<JamStateChanged>();
        fixture.VerifyStationUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateStationJamState_UnchangedJamState_PublishJamStateChanged()
    {
        const string station = "station";
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithStation(station);

        await fixture.PublishMessageAsync(new UpdateStationJamState(station.ToGuid(), false));

        await fixture.ShouldHavePublishedAsync<JamStateChanged>();
        fixture.VerifyStationUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateStationAdditionalInformation_StationUpdated()
    {
        const string station = "station";
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithStation(station);

        await fixture.PublishMessageAsync(new UpdateStationAdditionalInfo(station.ToGuid(), new Dictionary<string, object?> { { "NeedsCargoReporting", true } }));

        fixture.VerifyStationUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_MapSaved_NoRelatedAgvs_DoesNotAddStations()
    {
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithAgvsRelatedToNodes([]);

        await fixture.TestHarness.Bus.Publish(new Common.Messages.Maps.MapSaved(new Common.Domain.Models.Graphs.Map(Guid.NewGuid(), "mapId", DateTime.UtcNow, "image", 1, 1, 23, 0, 0)));

        await fixture.ShouldHaveConsumedAsync<Common.Messages.Maps.MapSaved>();
        fixture.VerifyStationsAdded(Times.Never());
    }

    [TestMethod]
    public async Task Consume_MapSaved_ValidData_AddsStations()
    {
        var fixture = new NodeStationCommandHandlerTestFixture()
            .WithAgvsRelatedToNodes([Guid.NewGuid()])
            .WithAgvsGraph();

        await fixture.TestHarness.Bus.Publish(new Common.Messages.Maps.MapSaved(new Common.Domain.Models.Graphs.Map(Guid.NewGuid(), "mapId", DateTime.UtcNow, "image", 1, 1, 23, 0, 0)));

        await fixture.ShouldHaveConsumedAsync<Common.Messages.Maps.MapSaved>();
        fixture.VerifyStationsAdded(Times.Once());
    }
}