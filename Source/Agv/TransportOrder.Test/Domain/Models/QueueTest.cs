using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class QueueTest
{
    [TestMethod]
    public void AddNode_ValidData_NodeAdded()
    {
        var queue = new Queue("a queue");
        const string nodeId = "a node id";
        queue.AddNode(nodeId, 0);

        queue.Nodes.Count.ShouldBe(1);
        var node = queue.Nodes.First();
        node.NodeId.ShouldBe(nodeId);
        node.Position.ShouldBe(0);
    }

    [TestMethod]
    public void AddNode_NodeWithIdAlreadyExists_ThrowsQueueNodeExistsException()
    {
        var queue = new Queue("a queue");
        const string nodeId = "a node id";
        queue.AddNode(nodeId, 0);

        Should.Throw<QueueNodeExistsException>(() => queue.AddNode(nodeId, 1));
    }

    [TestMethod]
    public void AddNode_NodeAtPositionAlreadyExists_ThrowsQueueNodeExistsException()
    {
        var queue = new Queue("a queue");
        const int position = 0;
        queue.AddNode("node id", position);

        Should.Throw<QueueNodeExistsException>(() => queue.AddNode("another node id", position));
    }

    [TestMethod]
    public void RemoveNode_ValidData_RemovesNode()
    {
        var queue = new Queue("a queue");
        const string nodeId = "a node id";
        queue.AddNode(nodeId, 0);

        queue.RemoveNode(nodeId);

        queue.Nodes.ShouldBeEmpty();
    }

    [TestMethod]
    public void RemoveNode_NodeWithIdDoesNotExist_ThrowsNodeNotFoundException()
    {
        var queue = new Queue("a queue");
        const string nodeId = "a node id";
        queue.AddNode(nodeId, 0);

        Should.Throw<NodeNotFoundException>(() => queue.RemoveNode("unknown"));
    }

    [TestMethod]
    public void TryEnterQueue_QueueIsFull_ReturnsNull()
    {
        var queue = QueueGenerator.GenerateFullQueue();

        queue.TryEnterQueue().ShouldBeNull();
    }

    [TestMethod]
    public void TryEnterQueue_ValidData_ReturnsEntryNode()
    {
        const int size = 0;
        var queue = QueueGenerator.GenerateEmptyQueue(nodes: 3, size: size);
        string entryNode = queue.Nodes.OrderBy(n => n.Position).Last().NodeId;
        queue.TryEnterQueue().ShouldBe(entryNode);
        queue.AgvFillQuantity.ShouldBe(size + 1);
    }

    [TestMethod]
    public void TryEnqueue_QueueIsEmpty_ReturnsTrueTargetIsFormerTail()
    {
        var queue = QueueGenerator.GenerateEmptyQueue(nodes: 3);
        var tail = queue.Tail!;
        tail.Position.ShouldBe(queue.Nodes.OrderBy(n => n.Position).First().Position);
        var agv = Guid.NewGuid();

        queue.TryEnqueue(agv, out string? targetNode).ShouldBeTrue();

        targetNode.ShouldBe(tail.NodeId);
        queue.Tail!.Position.ShouldBe(tail.Position + 1);
        queue.Nodes.Single(n => n.NodeId == targetNode).State!.IsReservedForAgv(agv).ShouldBeTrue();
    }

    [TestMethod]
    public void TryEnqueue_QueueIsFull_ReturnsEntryNode()
    {
        var queue = QueueGenerator.GenerateFullQueue();

        queue.TryEnqueue(Guid.NewGuid(), out string? targetNode).ShouldBeTrue();
        targetNode.ShouldBe(queue.EntryNode);
    }

    [TestMethod]
    public void TryEnqueue_AgvFillQuantityIsZero_ReturnsFalse()
    {
        var queue = QueueGenerator.GenerateEmptyQueue(size: 0);

        queue.TryEnqueue(Guid.NewGuid(), out string? targetNode).ShouldBeFalse();
        targetNode.ShouldBe(null);
    }

    [DataRow(NodeLockState.Occupied, NodeLockState.Reserved, NodeLockState.Free)]
    [DataRow(NodeLockState.Reserved, NodeLockState.Reserved, NodeLockState.Free)]
    [DataRow(NodeLockState.Occupied, NodeLockState.Occupied, NodeLockState.Free)]
    [DataTestMethod]
    public void TryEnqueue_LastSlotInQueue_ReturnsTrueTargetIsFormerTail(params NodeLockState[] lockStates)
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: lockStates);
        var tail = queue.Tail!;
        tail.Position.ShouldBe(queue.Nodes.MaxBy(q => q.Position)!.Position);
        var agv = Guid.NewGuid();

        queue.TryEnqueue(agv, out string? targetNode).ShouldBeTrue();

        targetNode.ShouldBe(tail.NodeId);
        queue.AllAgvsEntered.ShouldBeTrue();
        queue.Tail.ShouldBeNull();
    }

    [TestMethod]
    public void TryEnqueue_WithDuplications_ReturnsTarget()
    {
        var agv = Guid.NewGuid();
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Reserved, NodeLockState.Reserved, NodeLockState.Occupied]);
        foreach (var queueNode in queue.Nodes.Where(n => n.Position > 0))
        {
            queueNode.State!.AgvCorrelationId = agv;
        }

        var agvToEnqueue = Guid.NewGuid();
        queue.TryEnqueue(agvToEnqueue, out string? targetNode).ShouldBeTrue();
        targetNode.ShouldBe(queue.EntryNode);
        queue.AllAgvsEntered.ShouldBeTrue();
        queue.Nodes.Single(n => n.NodeId == targetNode).State!.IsReservedForAgv(agvToEnqueue).ShouldBeTrue();
    }

    [DataRow(NodeLockState.Occupied, NodeLockState.Free, NodeLockState.Free)]
    [DataRow(NodeLockState.Reserved, NodeLockState.Free, NodeLockState.Free)]
    [DataTestMethod]
    public void TryEnqueue_MultipleSlotsInQueue_ReturnsTrueTargetIsFormerTail(params NodeLockState[] lockStates)
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: lockStates);
        var tail = queue.Tail!;
        tail.Position.ShouldBe(1);
        var agv = Guid.NewGuid();

        queue.TryEnqueue(agv, out string? targetNode).ShouldBeTrue();

        targetNode.ShouldBe(tail.NodeId);
        queue.IsFull.ShouldBeFalse();
        queue.Tail!.Position.ShouldBe(tail.Position + 1);
        queue.Nodes.Single(n => n.NodeId == targetNode).State!.IsReservedForAgv(agv).ShouldBeTrue();
    }

    [TestMethod]
    public void TryDequeue_QueueIsEmpty_ReturnsNullNoAgvMoves()
    {
        var queue = QueueGenerator.GenerateEmptyQueue();

        queue.TryDequeue(out Dictionary<Guid, string> agvsToMove).ShouldBeNull();
        agvsToMove.ShouldBeEmpty();
    }

    [TestMethod]
    public void TryDequeue_QueueIsFull_ReturnsAgvFromHeadAllOthersMove()
    {
        var queue = QueueGenerator.GenerateFullQueue(nodes: 3);
        queue.IsFull.ShouldBeTrue();
        var head = queue.Head!;
        var dequeuedAgv = head.State!.AgvCorrelationId!.Value;
        head.Position.ShouldBe(queue.Nodes.OrderBy(n => n.Position).First().Position);
        var agvMovingFromPosition2To1 = queue.Nodes.Single(n => n.Position == 2).State!.AgvCorrelationId!.Value;
        var agvMovingFromPosition1ToHead = queue.Nodes.Single(n => n.Position == 1).State!.AgvCorrelationId!.Value;

        queue.TryDequeue(out Dictionary<Guid, string> agvsToMove).ShouldBe(dequeuedAgv);

        queue.IsFull.ShouldBeFalse();
        agvsToMove.Count.ShouldBe(2);
        agvsToMove[agvMovingFromPosition2To1].ShouldBe(queue.Nodes.Single(n => n.Position == 1).NodeId);
        agvsToMove[agvMovingFromPosition1ToHead].ShouldBe(head.NodeId);
        queue.Head.ShouldBeNull(); // all agvs on the move
        queue.Nodes.Single(n => n.Position == 2).State!.NodeState.ShouldBe(NodeLockState.Occupied);
        queue.Nodes.Single(n => n.Position == 1).State!.NodeState.ShouldBe(NodeLockState.Reserved);
        queue.Nodes.Single(n => n.Position == 0).State!.NodeState.ShouldBe(NodeLockState.Reserved);
    }

    [DataRow(NodeLockState.Reserved, NodeLockState.Free, NodeLockState.Free)]
    [DataRow(NodeLockState.Reserved, NodeLockState.Reserved, NodeLockState.Free)]
    [DataRow(NodeLockState.Reserved, NodeLockState.Reserved, NodeLockState.Reserved)]
    [DataRow(NodeLockState.Reserved, NodeLockState.Occupied, NodeLockState.Occupied)]
    [DataRow(NodeLockState.Reserved, NodeLockState.Occupied, NodeLockState.Free)]
    [DataRow(NodeLockState.Reserved, NodeLockState.Free)]
    [DataTestMethod]
    public void TryDequeue_HeadNodeNotOccupied_ReturnsNullNoNewAgvMoves(params NodeLockState[] lockStates)
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: lockStates);
        queue.Head.ShouldBeNull();

        queue.TryDequeue(out Dictionary<Guid, string> agvsToMove).ShouldBeNull();
        agvsToMove.ShouldBeEmpty();
    }

    [TestMethod]
    public void TryDequeue_OnlyHeadNodeTaken_ReturnsAgvFromHeadAllOthersMove()
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Occupied, NodeLockState.Free, NodeLockState.Free]);
        var head = queue.Head!;

        queue.TryDequeue(out Dictionary<Guid, string> agvsToMove).ShouldBe(head.State!.AgvCorrelationId);
        agvsToMove.ShouldBeEmpty();
    }

    [DataRow(NodeLockState.Occupied, NodeLockState.Reserved, NodeLockState.Free)]
    [DataRow(NodeLockState.Occupied, NodeLockState.Occupied, NodeLockState.Free)]
    [DataTestMethod]
    public void TryDequeue_QueueNotFull_ReturnsAgvFromHeadNoAgvMoves(params NodeLockState[] lockStates)
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: lockStates);
        var head = queue.Head!;
        var dequeuedAgv = head.State!.AgvCorrelationId!.Value;
        var movingAgv = queue.Nodes.Single(n => n.Position == head.Position + 1).State!.AgvCorrelationId!.Value;

        queue.TryDequeue(out Dictionary<Guid, string> agvsToMove).ShouldBe(dequeuedAgv);

        agvsToMove.Count.ShouldBe(1);
        agvsToMove[movingAgv].ShouldBe(head.NodeId);
    }

    [TestMethod]
    public void TryEscapeQueue_EmptyQueue_ReturnsNull()
    {
        var queue = QueueGenerator.GenerateEmptyQueue(nodes: 3);
        queue.TryEscapeQueue(Guid.NewGuid(), out Dictionary<Guid, string> agvsToMove).ShouldBeNull();
        agvsToMove.ShouldBeEmpty();
    }

    [TestMethod]
    public void TryEscapeQueue_AgvInMiddleOfQueueEscaping_ReturnsNodeIdMovedAgvs()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        var escapedNode = queue.Nodes.OrderBy(n => n.Position).Skip(1).First();
        var movingAgv = queue.Nodes.OrderBy(n => n.Position).Last().State!.AgvCorrelationId!.Value;

        queue.TryEscapeQueue(escapedNode.State!.AgvCorrelationId!.Value, out Dictionary<Guid, string> agvsToMove).ShouldBe(escapedNode.NodeId);
        agvsToMove.Count.ShouldBe(1);
        agvsToMove[movingAgv].ShouldBe(escapedNode.NodeId);
    }

    [TestMethod]
    public void TryEscapeQueue_AgvAtQueueEnd_ReturnsNodeIdNoMovedAgvs()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        var escapedNode = queue.Nodes.MaxBy(n => n.Position)!;

        queue.TryEscapeQueue(escapedNode.State!.AgvCorrelationId!.Value, out Dictionary<Guid, string> agvsToMove).ShouldBe(escapedNode.NodeId);
        agvsToMove.ShouldBeEmpty();
    }

    [TestMethod]
    public void IsEnqueued_AgvNotInQueue_ReturnsFalse()
    {
        var queue = QueueGenerator.GenerateEmptyQueue();

        queue.IsEnqueued(Guid.NewGuid()).ShouldBeFalse();
    }

    [DataRow(NodeLockState.Occupied, true)]
    [DataRow(NodeLockState.Reserved, true)]
    [DataRow(NodeLockState.Free, false)]
    [DataTestMethod]
    public void IsEnqueued_ValidData_ReturnsAccordingToNodeState(NodeLockState lockState, bool expectedResult)
    {
        var agv = Guid.NewGuid();
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [lockState, NodeLockState.Reserved, NodeLockState.Free]);
        if (lockState != NodeLockState.Free)
        {
            queue.Nodes.OrderBy(n => n.Position).First().State!.AgvCorrelationId = agv;
        }

        queue.IsEnqueued(agv).ShouldBe(expectedResult);
    }

    [TestMethod]
    public void IsAgvAtQueueHead_DifferentNode_False()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        var agv = queue.Head!.State!.AgvCorrelationId!.Value;
        queue.IsAgvAtQueueHead(agv, queue.Nodes.MaxBy(n => n.Position)!.NodeId).ShouldBeFalse();
    }

    [TestMethod]
    public void IsAgvAtQueueHead_DifferentAgvIsAtHead_False()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        string head = queue.Head!.NodeId;
        queue.IsAgvAtQueueHead(Guid.NewGuid(), head).ShouldBeFalse();
    }

    [TestMethod]
    public void IsAgvAtQueueHead_AgvIsAtHead_True()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        string head = queue.Head!.NodeId;
        var agv = queue.Head!.State!.AgvCorrelationId!.Value;
        queue.IsAgvAtQueueHead(agv, head).ShouldBeTrue();
    }

    [TestMethod]
    public void RemoveAgv_ValidData_ReducesAgvFillQuantity()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        int queueSize = queue.AgvFillQuantity;

        queue.RemoveAgv();
        queue.AgvFillQuantity.ShouldBe(queueSize - 1);
    }

    [TestMethod]
    public void RemoveAgv_EmptyQueue_DoesNotReduceAgvFillQuantity()
    {
        var queue = QueueGenerator.GenerateEmptyQueue(size: 0);
        int queueSize = queue.AgvFillQuantity;

        queue.RemoveAgv();
        queue.AgvFillQuantity.ShouldBe(queueSize);
    }
}