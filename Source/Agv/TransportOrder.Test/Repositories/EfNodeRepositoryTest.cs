using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class GraphNodeRepositoryTest
{
    private static Mapper Mapper => MappingHelper.GetMapper(new GraphProfile());

    [TestMethod]
    public async Task GetNodeStatesForMapAsync_UnknownMap_ShouldBeEmpty()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        List<NodeLock> nodeLocks = agvGraph.Nodes.Select(n => new NodeLock(n, NodeLockState.Occupied, true, Guid.NewGuid(), DateTime.UtcNow)).ToList();
        fixture.AddGraph(agvGraph);
        fixture.AddNodeLocks(nodeLocks);

        (await fixture.Repository.GetNodeStatesForMapAsync("unknown".ToGuid())).IsNullOrEmpty().ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetNodeStatesForMapAsync_ValidData_ReturnsNodeLocks()
    {
        var mapCorrelationId = Guid.NewGuid();
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        List<NodeLock> nodeLocks = agvGraph.Nodes.Select(n => new NodeLock(n, NodeLockState.Occupied, true, Guid.NewGuid(), DateTime.UtcNow)).ToList();
        fixture.AddGraph(agvGraph);
        fixture.AddMap(MapGenerator.Generate(mapCorrelationId, nodes: Mapper.Map<ICollection<GraphNode>>(agvGraph.Nodes)));
        fixture.AddNodeLocks(nodeLocks);

        List<NodeLock> actualNodeStates = (await fixture.Repository.GetNodeStatesForMapAsync(mapCorrelationId)).Select(x => x.NodeLock).ToList();

        actualNodeStates.ShouldBe(nodeLocks, ignoreOrder: true);
    }

    [TestMethod]
    public async Task GetNodeStatesForMapAsync_NotAllNodesHaveLockState_ReturnsNodeStatesForAllNodes()
    {
        var mapCorrelationId = Guid.NewGuid();
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph();
        List<NodeLock> nodeLocks = agvGraph.Nodes.Take(1).Select(n => new NodeLock(n, NodeLockState.Occupied, true, Guid.NewGuid(), DateTime.UtcNow)).ToList();
        fixture.AddGraph(agvGraph);
        fixture.AddMap(MapGenerator.Generate(mapCorrelationId, nodes: Mapper.Map<ICollection<GraphNode>>(agvGraph.Nodes)));
        fixture.AddNodeLocks(nodeLocks);

        List<Station> stations = agvGraph.Nodes.Take(5).Select(n => new Station(n.Name, AgvGraph.NodeType.PickDrop, new List<AgvGraph.NodeType>())).ToList();
        fixture.AddStations(stations);

        ICollection<(NodeLock NodeLock, Guid? StationId, string? LockReason, string? LockDescription, string? LockedByUser, string NodeName)> actualNodeStates = await fixture.Repository.GetNodeStatesForMapAsync(mapCorrelationId);
        foreach (var agvGraphNode in agvGraph.Nodes)
        {
            (NodeLock NodeLock, Guid? StationId, string? LockReason, string? LockDescription, string? LockedByUser, string NodeName) actualNodeState = actualNodeStates.Single(ns => ns.NodeLock.NodeId == agvGraphNode.Id);
            if (nodeLocks.Select(nl => nl.NodeId).Contains(agvGraphNode.Id))
            {
                actualNodeState.NodeLock.ShouldBe(nodeLocks.Single(nl => nl.NodeId == actualNodeState.NodeLock.NodeId));
                continue;
            }
            actualNodeState.NodeLock.IsLocked.ShouldBeFalse();
            actualNodeState.NodeLock.NodeState.ShouldBe(NodeLockState.Free);
            actualNodeState.NodeLock.AgvCorrelationId.ShouldBeNull();
        }
    }

    [TestMethod]
    public async Task GetNodeAsync_NodeWithLock_ReturnsNodeWithLock()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        var nodeLock = new NodeLock(node, NodeLockState.Occupied, true, agvCorrelationId, DateTime.UtcNow);
        fixture.AddNode(node);
        fixture.AddNodeLock(nodeLock);

        var result = await fixture.Repository.GetNodeAsync(node.Id);

        result.Id.ShouldBe(node.Id);
        result.Name.ShouldBe(node.Name);
        result.Lock.ShouldNotBeNull();
        result.Lock!.NodeState.ShouldBe(NodeLockState.Occupied);
        result.Lock.IsLocked.ShouldBeTrue();
        result.Lock.AgvCorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task GetNodeAsync_NodeWithoutLock_ReturnsNodeWithNullLock()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        fixture.AddNode(node);

        var result = await fixture.Repository.GetNodeAsync(node.Id);

        result.Id.ShouldBe(node.Id);
        result.Name.ShouldBe(node.Name);
        result.Lock.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetNodesForAgv_AgvHasNodes_ReturnsNodes()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var agvCorrelationId = Guid.NewGuid();
        var node1 = new GraphNode { Id = Guid.NewGuid(), Name = "Node1" };
        var node2 = new GraphNode { Id = Guid.NewGuid(), Name = "Node2" };
        var node3 = new GraphNode { Id = Guid.NewGuid(), Name = "Node3" };
        fixture.AddNodes([node1, node2, node3]);
        fixture.AddNodeLock(new NodeLock(node1, NodeLockState.Occupied, false, agvCorrelationId, DateTime.UtcNow));
        fixture.AddNodeLock(new NodeLock(node2, NodeLockState.Reserved, false, agvCorrelationId, DateTime.UtcNow));
        fixture.AddNodeLock(new NodeLock(node3, NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow));

        ICollection<GraphNode> result = await fixture.Repository.GetNodesForAgv(agvCorrelationId);

        result.Count.ShouldBe(2);
        result.ShouldContain(n => n.Id == node1.Id);
        result.ShouldContain(n => n.Id == node2.Id);
        result.ShouldNotContain(n => n.Id == node3.Id);
    }

    [TestMethod]
    public async Task GetNodesForAgv_AgvHasNoNodes_ReturnsEmptyCollection()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var agvCorrelationId = Guid.NewGuid();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "Node1" };
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow));

        ICollection<GraphNode> result = await fixture.Repository.GetNodesForAgv(agvCorrelationId);

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetLockedNodes_HasLockedNodes_ReturnsLockedNodeIds()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node1 = new GraphNode { Id = Guid.NewGuid(), Name = "Node1" };
        var node2 = new GraphNode { Id = Guid.NewGuid(), Name = "Node2" };
        var node3 = new GraphNode { Id = Guid.NewGuid(), Name = "Node3" };
        fixture.AddNodes([node1, node2, node3]);
        fixture.AddNodeLock(new NodeLock(node1, NodeLockState.Occupied, true, Guid.NewGuid(), DateTime.UtcNow));
        fixture.AddNodeLock(new NodeLock(node2, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow));
        fixture.AddNodeLock(new NodeLock(node3, NodeLockState.Occupied, true, Guid.NewGuid(), DateTime.UtcNow));

        IReadOnlyCollection<Guid>? result = await fixture.Repository.GetLockedNodes();

        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain(node1.Id);
        result.ShouldNotContain(node2.Id);
        result.ShouldContain(node3.Id);
    }

    [TestMethod]
    public async Task GetLockedNodes_NoLockedNodes_ReturnsEmptyCollection()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "Node1" };
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow));

        IReadOnlyCollection<Guid>? result = await fixture.Repository.GetLockedNodes();

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task UpdateLock_LockUnlockedNode_LocksNode()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeTrue();
        var updatedLock = fixture.GetNodeLock(node.Id);
        updatedLock.ShouldNotBeNull();
        updatedLock.IsLocked.ShouldBeTrue();
    }

    [TestMethod]
    public async Task UpdateLock_LockAlreadyLockedNode_ReturnsFalse()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public async Task UpdateLock_UnlockLockedNode_UnlocksNode()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeTrue();
        var updatedLock = fixture.GetNodeLock(node.Id);
        updatedLock.ShouldNotBeNull();
        updatedLock.IsLocked.ShouldBeFalse();
    }

    [TestMethod]
    public async Task UpdateLock_UnlockNotLockedNode_ReturnsFalse()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public async Task UpdateLock_ReserveFreeNode_ReservesNode()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Reserved, false, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeTrue();
        var updatedLock = fixture.GetNodeLock(node.Id);
        updatedLock.ShouldNotBeNull();
        updatedLock.NodeState.ShouldBe(NodeLockState.Reserved);
        updatedLock.AgvCorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task UpdateLock_ReserveLockedNode_ReturnsFalse()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Reserved, true, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public async Task UpdateLock_ReserveOccupiedNode_ReturnsFalse()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Reserved, false, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public async Task UpdateLock_ForceReserveFreeNode_ReservesNode()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Reserved, false, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node, force: true);

        result.ShouldBeTrue();
        var updatedLock = fixture.GetNodeLock(node.Id);
        updatedLock.ShouldNotBeNull();
        updatedLock.NodeState.ShouldBe(NodeLockState.Reserved);
    }

    [TestMethod]
    public async Task UpdateLock_ForceReserveOccupiedNode_ReservesNode()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Reserved, false, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node, force: true);

        result.ShouldBeTrue();
        var updatedLock = fixture.GetNodeLock(node.Id);
        updatedLock.ShouldNotBeNull();
        updatedLock.NodeState.ShouldBe(NodeLockState.Reserved);
    }

    [TestMethod]
    public async Task UpdateLock_ForceReserveLockedNode_ReturnsFalse()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Reserved, true, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node, force: true);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public async Task UpdateLock_ForceReserveLockedNodeWithIgnoreLocked_ReservesNode()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Reserved, false, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node, force: true, ignoreLocked: true);

        result.ShouldBeTrue();
        var updatedLock = fixture.GetNodeLock(node.Id);
        updatedLock.ShouldNotBeNull();
        updatedLock.NodeState.ShouldBe(NodeLockState.Reserved);
    }

    [TestMethod]
    public async Task UpdateLock_OccupyFreeNode_OccupiesNode()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Occupied, false, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeTrue();
        var updatedLock = fixture.GetNodeLock(node.Id);
        updatedLock.ShouldNotBeNull();
        updatedLock.NodeState.ShouldBe(NodeLockState.Occupied);
        updatedLock.AgvCorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task UpdateLock_OccupyReservedNode_OccupiesNode()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Reserved, false, agvCorrelationId, DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Occupied, false, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeTrue();
        var updatedLock = fixture.GetNodeLock(node.Id);
        updatedLock.ShouldNotBeNull();
        updatedLock.NodeState.ShouldBe(NodeLockState.Occupied);
    }

    [TestMethod]
    public async Task UpdateLock_OccupyAlreadyOccupiedNode_ReturnsFalse()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Occupied, false, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public async Task UpdateLock_OccupyLockedNode_ReturnsFalse()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Occupied, true, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public async Task UpdateLock_ForceOccupyAlreadyOccupiedNode_OccupiesNode()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Occupied, false, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node, force: true);

        result.ShouldBeTrue();
        var updatedLock = fixture.GetNodeLock(node.Id);
        updatedLock.ShouldNotBeNull();
        updatedLock.AgvCorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task UpdateLock_ForceOccupyLockedNode_ReturnsFalse()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Occupied, true, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node, force: true);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public async Task UpdateLock_ForceOccupyLockedNodeWithIgnoreLocked_OccupiesNode()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Occupied, false, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node, force: true, ignoreLocked: true);

        result.ShouldBeTrue();
        var updatedLock = fixture.GetNodeLock(node.Id);
        updatedLock.ShouldNotBeNull();
        updatedLock.NodeState.ShouldBe(NodeLockState.Occupied);
    }

    [TestMethod]
    public async Task UpdateLock_FreeOccupiedNode_FreesNode()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeTrue();
        var updatedLock = fixture.GetNodeLock(node.Id);
        updatedLock.ShouldNotBeNull();
        updatedLock.NodeState.ShouldBe(NodeLockState.Free);
        updatedLock.AgvCorrelationId.ShouldBeNull();
    }

    [TestMethod]
    public async Task UpdateLock_FreeReservedNode_FreesNode()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeTrue();
        var updatedLock = fixture.GetNodeLock(node.Id);
        updatedLock.ShouldNotBeNull();
        updatedLock.NodeState.ShouldBe(NodeLockState.Free);
    }

    [TestMethod]
    public async Task UpdateLock_FreeAlreadyFreeNode_ReturnsFalse()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        fixture.AddNode(node);
        fixture.AddNodeLock(new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow));
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public async Task UpdateLock_LockNodeWithoutExistingLock_CreatesLock()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        fixture.AddNode(node);
        node.Lock = new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeTrue();
        var createdLock = fixture.GetNodeLock(node.Id);
        createdLock.ShouldNotBeNull();
        createdLock.IsLocked.ShouldBeTrue();
    }

    [TestMethod]
    public async Task UpdateLock_ReserveNodeWithoutExistingLock_CreatesReservedLock()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        node.Lock = new NodeLock(node, NodeLockState.Reserved, false, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeTrue();
        var createdLock = fixture.GetNodeLock(node.Id);
        createdLock.ShouldNotBeNull();
        createdLock.NodeState.ShouldBe(NodeLockState.Reserved);
        createdLock.AgvCorrelationId.ShouldBe(agvCorrelationId);
    }

    [TestMethod]
    public async Task UpdateLock_OccupyNodeWithoutExistingLock_CreatesOccupiedLock()
    {
        await using var fixture = new GraphNodeRepositoryTestFixture();
        var node = new GraphNode { Id = Guid.NewGuid(), Name = "TestNode" };
        var agvCorrelationId = Guid.NewGuid();
        fixture.AddNode(node);
        node.Lock = new NodeLock(node, NodeLockState.Occupied, false, agvCorrelationId, DateTime.UtcNow);

        bool result = await fixture.Repository.UpdateLock(node);

        result.ShouldBeTrue();
        var createdLock = fixture.GetNodeLock(node.Id);
        createdLock.ShouldNotBeNull();
        createdLock.NodeState.ShouldBe(NodeLockState.Occupied);
        createdLock.AgvCorrelationId.ShouldBe(agvCorrelationId);
    }
}