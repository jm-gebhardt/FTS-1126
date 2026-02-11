using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class NodesExtensionTest
{
    [TestMethod]
    public void SelectById_ValidData_ReturnsNode()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "id");
        var nodes = new List<GraphNode> { node };
        nodes.SelectById(node.Id).ShouldBe(node);
    }

    [TestMethod]
    public void SelectById_NodeNotInCollection_ThrowsNodeNotFoundException()
    {
        Should.Throw<NodeNotFoundException>(() => new List<GraphNode>().SelectById(Guid.Empty));
    }

    [TestMethod]
    public void IsFree_LockIsNull_ReturnsTrue()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = null;
        node.IsFree.ShouldBeTrue();
    }

    [TestMethod]
    public void IsFree_NodeStateIsFree_ReturnsTrue()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);
        node.IsFree.ShouldBeTrue();
    }

    [TestMethod]
    public void IsFree_NodeStateIsReserved_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow);
        node.IsFree.ShouldBeFalse();
    }

    [TestMethod]
    public void IsFree_NodeStateIsOccupied_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow);
        node.IsFree.ShouldBeFalse();
    }

    [TestMethod]
    public void IsLocked_LockIsNull_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = null;
        node.IsLocked.ShouldBeFalse();
    }

    [TestMethod]
    public void IsLocked_IsLockedFalse_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);
        node.IsLocked.ShouldBeFalse();
    }

    [TestMethod]
    public void IsLocked_IsLockedTrue_ReturnsTrue()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow);
        node.IsLocked.ShouldBeTrue();
    }

    [TestMethod]
    public void IsOccupied_LockIsNull_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = null;
        node.IsOccupied.ShouldBeFalse();
    }

    [TestMethod]
    public void IsOccupied_NodeStateIsFree_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);
        node.IsOccupied.ShouldBeFalse();
    }

    [TestMethod]
    public void IsOccupied_NodeStateIsReserved_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow);
        node.IsOccupied.ShouldBeFalse();
    }

    [TestMethod]
    public void IsOccupied_NodeStateIsOccupied_ReturnsTrue()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow);
        node.IsOccupied.ShouldBeTrue();
    }

    [TestMethod]
    public void IsReserved_LockIsNull_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = null;
        node.IsReserved.ShouldBeFalse();
    }

    [TestMethod]
    public void IsReserved_NodeStateIsFree_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);
        node.IsReserved.ShouldBeFalse();
    }

    [TestMethod]
    public void IsReserved_NodeStateIsReserved_ReturnsTrue()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow);
        node.IsReserved.ShouldBeTrue();
    }

    [TestMethod]
    public void IsReserved_NodeStateIsOccupied_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow);
        node.IsReserved.ShouldBeFalse();
    }

    [TestMethod]
    public void Reserve_LockIsNull_ReservesNode()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = null;
        var agvId = Guid.NewGuid();

        bool result = node.Reserve(agvId);

        result.ShouldBeTrue();
        node.Lock.ShouldNotBeNull();
        node.Lock.NodeState.ShouldBe(NodeLockState.Reserved);
        node.Lock.AgvCorrelationId.ShouldBe(agvId);
    }

    [TestMethod]
    public void Reserve_NodeIsFree_ReservesNode()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Reserve(agvId);

        result.ShouldBeTrue();
    }

    [TestMethod]
    public void Reserve_NodeIsLocked_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Reserve(agvId);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public void Reserve_NodeIsReserved_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Reserve(agvId);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public void Reserve_NodeIsOccupied_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Reserve(agvId);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public void Reserve_ForceTrue_ReservesRegardlessOfState()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Occupied, true, Guid.NewGuid(), DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Reserve(agvId, force: true);

        result.ShouldBeTrue();
        node.Lock.NodeState.ShouldBe(NodeLockState.Reserved);
        node.Lock.AgvCorrelationId.ShouldBe(agvId);
    }

    [TestMethod]
    public void Reserve_IgnoreLockedTrueAndNodeIsFree_ReservesNode()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Reserve(agvId, ignoreLocked: true);

        result.ShouldBeTrue();
        node.Lock.NodeState.ShouldBe(NodeLockState.Reserved);
    }

    [TestMethod]
    public void Reserve_IgnoreLockedTrueAndNodeIsNotFree_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Occupied, true, Guid.NewGuid(), DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Reserve(agvId, ignoreLocked: true);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public void Reserve_IgnoreLockedTrueAndNodeIsNotLocked_ReservesNode()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Reserve(agvId, ignoreLocked: true);

        result.ShouldBeTrue();
        node.Lock.NodeState.ShouldBe(NodeLockState.Reserved);
    }

    [TestMethod]
    public void Occupy_LockIsNull_OccupiesNode()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = null;
        var agvId = Guid.NewGuid();

        bool result = node.Occupy(agvId);

        result.ShouldBeTrue();
        node.Lock.ShouldNotBeNull();
        node.Lock.NodeState.ShouldBe(NodeLockState.Occupied);
        node.Lock.AgvCorrelationId.ShouldBe(agvId);
    }

    [TestMethod]
    public void Occupy_NodeIsFree_OccupiesNode()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Occupy(agvId);

        result.ShouldBeTrue();
    }

    [TestMethod]
    public void Occupy_NodeIsReserved_OccupiesNode()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Occupy(agvId);

        result.ShouldBeTrue();
    }

    [TestMethod]
    public void Occupy_NodeIsLocked_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Occupy(agvId);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public void Occupy_NodeIsOccupied_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Occupy(agvId);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public void Occupy_ForceTrue_OccupiesRegardlessOfState()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Occupied, true, Guid.NewGuid(), DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Occupy(agvId, force: true);

        result.ShouldBeTrue();
        node.Lock.NodeState.ShouldBe(NodeLockState.Occupied);
        node.Lock.AgvCorrelationId.ShouldBe(agvId);
    }

    [TestMethod]
    public void Occupy_IgnoreLockedTrueAndNodeIsNotOccupied_OccupiesNode()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Reserved, true, Guid.NewGuid(), DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Occupy(agvId, ignoreLocked: true);

        result.ShouldBeTrue();
        node.Lock.NodeState.ShouldBe(NodeLockState.Occupied);
    }

    [TestMethod]
    public void Occupy_IgnoreLockedTrueAndNodeIsOccupied_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Occupied, true, Guid.NewGuid(), DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Occupy(agvId, ignoreLocked: true);

        result.ShouldBeFalse();
    }

    [TestMethod]
    public void Occupy_IgnoreLockedTrueAndNodeIsNotLocked_OccupiesNode()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);
        var agvId = Guid.NewGuid();

        bool result = node.Occupy(agvId, ignoreLocked: true);

        result.ShouldBeTrue();
        node.Lock.NodeState.ShouldBe(NodeLockState.Occupied);
    }

    [TestMethod]
    public void Free_NodeIsAlreadyFree_ReturnsTrue()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = null;

        bool result = node.Free();

        result.ShouldBeTrue();
    }

    [TestMethod]
    public void Free_NodeIsReserved_FreesNode()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Reserved, false, Guid.NewGuid(), DateTime.UtcNow);

        bool result = node.Free();

        result.ShouldBeTrue();
        node.Lock.NodeState.ShouldBe(NodeLockState.Free);
        node.Lock.AgvCorrelationId.ShouldBeNull();
    }

    [TestMethod]
    public void Free_NodeIsOccupied_FreesNode()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow);

        bool result = node.Free();

        result.ShouldBeTrue();
        node.Lock.NodeState.ShouldBe(NodeLockState.Free);
        node.Lock.AgvCorrelationId.ShouldBeNull();
    }

    [TestMethod]
    public void Free_NodeIsLocked_PreservesIsLockedFlag()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Occupied, true, Guid.NewGuid(), DateTime.UtcNow);

        bool result = node.Free();

        result.ShouldBeTrue();
        node.Lock.NodeState.ShouldBe(NodeLockState.Free);
        node.Lock.IsLocked.ShouldBeTrue();
    }

    [TestMethod]
    public void Lock_NodeNotLocked_LocksNode()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);

        bool result = node.Lock();

        result.ShouldBeTrue();
        node.Lock.IsLocked.ShouldBeTrue();
    }

    [TestMethod]
    public void Lock_NodeAlreadyLocked_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow);

        bool result = node.Lock();

        result.ShouldBeFalse();
    }

    [TestMethod]
    public void Lock_LockIsNull_LocksNode()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = null;

        bool result = node.Lock();

        result.ShouldBeTrue();
        node.Lock.ShouldNotBeNull();
        node.Lock.IsLocked.ShouldBeTrue();
        node.Lock.NodeState.ShouldBe(NodeLockState.Free);
    }

    [TestMethod]
    public void Lock_PreservesNodeState()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        var agvId = Guid.NewGuid();
        node.Lock = new NodeLock(node, NodeLockState.Reserved, false, agvId, DateTime.UtcNow);

        bool result = node.Lock();

        result.ShouldBeTrue();
        node.Lock.IsLocked.ShouldBeTrue();
        node.Lock.NodeState.ShouldBe(NodeLockState.Reserved);
        node.Lock.AgvCorrelationId.ShouldBe(agvId);
    }

    [TestMethod]
    public void Unlock_NodeIsLocked_UnlocksNode()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, true, null, DateTime.UtcNow);

        bool result = node.Unlock();

        result.ShouldBeTrue();
        node.Lock.IsLocked.ShouldBeFalse();
    }

    [TestMethod]
    public void Unlock_NodeNotLocked_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = new NodeLock(node, NodeLockState.Free, false, null, DateTime.UtcNow);

        bool result = node.Unlock();

        result.ShouldBeFalse();
    }

    [TestMethod]
    public void Unlock_LockIsNull_ReturnsFalse()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        node.Lock = null;

        bool result = node.Unlock();

        result.ShouldBeFalse();
    }

    [TestMethod]
    public void Unlock_PreservesNodeState()
    {
        var node = NodeGenerator.GenerateNode(nodeName: "node1");
        var agvId = Guid.NewGuid();
        node.Lock = new NodeLock(node, NodeLockState.Occupied, true, agvId, DateTime.UtcNow);

        bool result = node.Unlock();

        result.ShouldBeTrue();
        node.Lock.IsLocked.ShouldBeFalse();
        node.Lock.NodeState.ShouldBe(NodeLockState.Occupied);
        node.Lock.AgvCorrelationId.ShouldBe(agvId);
    }
}