using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class QueueTest
{
    private static readonly Guid _nodeId = "a node id".ToGuid();

    [TestMethod]
    public void AddNode_ValidData_NodeAdded()
    {
        var queue = new Queue("a queue");
        queue.AddNode(_nodeId, 0);

        queue.Nodes.Count.ShouldBe(1);
        var node = queue.Nodes.First();
        node.NodeId.ShouldBe(_nodeId);
        node.Position.ShouldBe(0);
    }

    [TestMethod]
    public void AddNode_NodeWithIdAlreadyExists_ThrowsQueueNodeExistsException()
    {
        var queue = new Queue("a queue");
        queue.AddNode(_nodeId, 0);

        Should.Throw<QueueNodeExistsException>(() => queue.AddNode(_nodeId, 1));
    }

    [TestMethod]
    public void AddNode_NodeAtPositionAlreadyExists_ThrowsQueueNodeExistsException()
    {
        var queue = new Queue("a queue");
        const int position = 0;
        queue.AddNode(_nodeId, position);

        Should.Throw<QueueNodeExistsException>(() => queue.AddNode("another node id".ToGuid(), position));
    }

    [TestMethod]
    public void RemoveNode_ValidData_RemovesNode()
    {
        var queue = new Queue("a queue");
        queue.AddNode(_nodeId, 0);

        queue.RemoveNode(_nodeId);

        queue.Nodes.ShouldBeEmpty();
    }

    [TestMethod]
    public void RemoveNode_NodeWithIdDoesNotExist_ThrowsNodeNotFoundException()
    {
        var queue = new Queue("a queue");
        queue.AddNode(_nodeId, 0);

        Should.Throw<NodeNotFoundException>(() => queue.RemoveNode("unknown".ToGuid()));
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
        var entryNode = queue.Nodes.OrderBy(n => n.Position).Last().NodeId;
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

        queue.TryEnterQueue();
        queue.TryEnqueue(agv, out Guid? targetNode).ShouldBeTrue();

        targetNode.ShouldBe(tail.NodeId);
        queue.Tail!.Position.ShouldBe(tail.Position + 1);
        queue.Nodes.Single(n => n.NodeId == targetNode).ReservedAgv.ShouldBe(agv);
    }

    [TestMethod]
    public void TryEnqueue_QueueIsFull_ReturnsEntryNode()
    {
        var queue = QueueGenerator.GenerateFullQueue();

        queue.TryEnqueue(Guid.NewGuid(), out Guid? targetNode).ShouldBeTrue();
        targetNode.ShouldBe(queue.EntryNode);
    }

    [TestMethod]
    public void TryEnqueue_AgvFillQuantityIsZero_ReturnsFalse()
    {
        var queue = QueueGenerator.GenerateEmptyQueue(size: 0);

        queue.TryEnqueue(Guid.NewGuid(), out Guid? targetNode).ShouldBeFalse();
        targetNode.ShouldBe(null);
    }

    [DataRow(NodeLockState.Occupied, NodeLockState.Reserved, NodeLockState.Free)]
    [DataRow(NodeLockState.Reserved, NodeLockState.Reserved, NodeLockState.Free)]
    [DataRow(NodeLockState.Occupied, NodeLockState.Occupied, NodeLockState.Free)]
    [TestMethod]
    public void TryEnqueue_LastSlotInQueue_ReturnsTrueTargetIsFormerTail(params NodeLockState[] lockStates)
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: lockStates);
        var tail = queue.Tail!;
        tail.Position.ShouldBe(queue.Nodes.MaxBy(q => q.Position)!.Position);
        var agv = Guid.NewGuid();

        queue.TryEnqueue(agv, out Guid? targetNode).ShouldBeTrue();

        targetNode.ShouldBe(tail.NodeId);
        queue.AllAgvsEntered.ShouldBeTrue();
        queue.Tail.ShouldBeNull();
    }

    [DataRow(NodeLockState.Occupied, NodeLockState.Free, NodeLockState.Free)]
    [DataRow(NodeLockState.Reserved, NodeLockState.Free, NodeLockState.Free)]
    [TestMethod]
    public void TryEnqueue_MultipleSlotsInQueue_ReturnsTrueTargetIsFormerTail(params NodeLockState[] lockStates)
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: lockStates);
        var tail = queue.Tail!;
        tail.Position.ShouldBe(1);
        var agv = Guid.NewGuid();

        queue.TryEnqueue(agv, out Guid? targetNode).ShouldBeTrue();

        targetNode.ShouldBe(tail.NodeId);
        queue.IsFull.ShouldBeFalse();
        queue.Tail!.Position.ShouldBe(tail.Position + 1);
        queue.Nodes.Single(n => n.NodeId == targetNode).ReservedAgv.ShouldBe(agv);
    }

    [TestMethod]
    public void TryDequeue_QueueIsEmpty_ReturnsNullNoAgvMoves()
    {
        var queue = QueueGenerator.GenerateEmptyQueue();

        queue.TryDequeue(out Dictionary<Guid, Guid> agvsToMove).ShouldBeNull();
        agvsToMove.ShouldBeEmpty();
    }

    [TestMethod]
    public void TryDequeue_QueueIsFull_ReturnsAgvFromHeadAllOthersMove()
    {
        var queue = QueueGenerator.GenerateFullQueue(nodes: 3);
        queue.IsFull.ShouldBeTrue();
        queue.AgvFillQuantity.ShouldBe(3);
        var head = queue.Head!;
        var dequeuedAgv = head.OccupiedAgv!.Value;
        head.Position.ShouldBe(queue.Nodes.OrderBy(n => n.Position).First().Position);
        var agvMovingFromPosition2To1 = queue.Nodes.Single(n => n.Position == 2).OccupiedAgv!.Value;
        var agvMovingFromPosition1ToHead = queue.Nodes.Single(n => n.Position == 1).OccupiedAgv!.Value;

        queue.TryDequeue(out Dictionary<Guid, Guid> agvsToMove).ShouldBe(dequeuedAgv);

        queue.IsFull.ShouldBeFalse();
        queue.AgvFillQuantity.ShouldBe(2);
        agvsToMove.Count.ShouldBe(2);
        agvsToMove[agvMovingFromPosition2To1].ShouldBe(queue.Nodes.Single(n => n.Position == 1).NodeId);
        agvsToMove[agvMovingFromPosition1ToHead].ShouldBe(head.NodeId);
        queue.Head.ShouldBeNull(); // all agvs on the move
        queue.Nodes.Single(n => n.Position == 2).GetCurrentAgv().ShouldBeNull();
        queue.Nodes.Single(n => n.Position == 1).GetCurrentAgv().ShouldBe(agvMovingFromPosition2To1);
        queue.Nodes.Single(n => n.Position == 0).GetCurrentAgv().ShouldBe(agvMovingFromPosition1ToHead);
    }

    [DataRow(NodeLockState.Reserved, NodeLockState.Free, NodeLockState.Free)]
    [DataRow(NodeLockState.Reserved, NodeLockState.Reserved, NodeLockState.Free)]
    [DataRow(NodeLockState.Reserved, NodeLockState.Reserved, NodeLockState.Reserved)]
    [DataRow(NodeLockState.Reserved, NodeLockState.Occupied, NodeLockState.Occupied)]
    [DataRow(NodeLockState.Reserved, NodeLockState.Occupied, NodeLockState.Free)]
    [DataRow(NodeLockState.Reserved, NodeLockState.Free)]
    [TestMethod]
    public void TryDequeue_HeadNodeNotOccupied_ReturnsNullNoNewAgvMoves(params NodeLockState[] lockStates)
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: lockStates);
        queue.Head.ShouldBeNull();

        queue.TryDequeue(out Dictionary<Guid, Guid> agvsToMove).ShouldBeNull();
        agvsToMove.ShouldBeEmpty();
    }

    [TestMethod]
    public void TryDequeue_OnlyHeadNodeTaken_ReturnsAgvFromHeadAllOthersMove()
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Occupied, NodeLockState.Free, NodeLockState.Free]);
        var head = queue.Head!;
        var agvAtQueueHead = new Guid(head.OccupiedAgv!.Value.ToString());

        queue.TryDequeue(out Dictionary<Guid, Guid> agvsToMove).ShouldBe(agvAtQueueHead);
        agvsToMove.ShouldBeEmpty();
    }

    [DataRow(NodeLockState.Occupied, NodeLockState.Reserved, NodeLockState.Free)]
    [DataRow(NodeLockState.Occupied, NodeLockState.Occupied, NodeLockState.Free)]
    [TestMethod]
    public void TryDequeue_QueueNotFull_ReturnsAgvFromHeadNoAgvMoves(params NodeLockState[] lockStates)
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: lockStates);
        var head = queue.Head!;

        var dequeuedAgv = new Guid(head.OccupiedAgv!.Value.ToString());
        var movingAgv = queue.Nodes.Single(n => n.Position == head.Position + 1).GetCurrentAgv()!.Value;

        queue.TryDequeue(out Dictionary<Guid, Guid> agvsToMove).ShouldBe(dequeuedAgv);

        agvsToMove.Count.ShouldBe(1);
        agvsToMove[movingAgv].ShouldBe(head.NodeId);
    }

    [TestMethod]
    public void TryEscapeQueue_EmptyQueue_ReturnsNull()
    {
        var queue = QueueGenerator.GenerateEmptyQueue(nodes: 3);
        queue.TryEscapeQueue(Guid.NewGuid(), out Dictionary<Guid, Guid> agvsToMove).ShouldBeNull();
        agvsToMove.ShouldBeEmpty();
    }

    [TestMethod]
    public void TryEscapeQueue_AgvInMiddleOfQueueEscaping_ReturnsNodeIdMovedAgvs()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        var escapedNode = queue.Nodes.OrderBy(n => n.Position).Skip(1).First();
        var movingAgv = queue.Nodes.OrderBy(n => n.Position).Last().OccupiedAgv!.Value;

        queue.TryEscapeQueue(escapedNode.OccupiedAgv!.Value, out Dictionary<Guid, Guid> agvsToMove).ShouldBe(escapedNode.NodeId);
        agvsToMove.Count.ShouldBe(1);
        agvsToMove[movingAgv].ShouldBe(escapedNode.NodeId);
    }

    [TestMethod]
    public void TryEscapeQueue_AgvAtQueueEnd_ReturnsNodeIdNoMovedAgvs()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        var escapedNode = queue.Nodes.MaxBy(n => n.Position)!;

        queue.TryEscapeQueue(escapedNode.OccupiedAgv!.Value, out Dictionary<Guid, Guid> agvsToMove).ShouldBe(escapedNode.NodeId);
        agvsToMove.ShouldBeEmpty();
    }

    [TestMethod]
    public void TryEscapeQueue_AgvReservedForHeadEscapes_AgvRemovedFromQueue()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        // Escape initiated by head evacuation
        var escapedNode = queue.Head!;
        queue.TryEscapeQueue(escapedNode.OccupiedAgv!.Value, out Dictionary<Guid, Guid> agvsToMoveFirstEscape).ShouldBe(escapedNode.NodeId);
        agvsToMoveFirstEscape.Count.ShouldBe(2);

        Guid? agvReservedForHead = queue.Nodes.MinBy(n => n.Position)!.ReservedAgv;
        agvReservedForHead.ShouldNotBeNull();
        //Next head evacuation while move from first has not started yet
        queue.TryEscapeQueue(agvReservedForHead.Value, out Dictionary<Guid, Guid> agvsToMoveSecondEscape).ShouldBe(escapedNode.NodeId);
        queue.Nodes.MinBy(n => n.Position)!.GetCurrentAgv().ShouldNotBe(agvReservedForHead);
        agvsToMoveSecondEscape.Count.ShouldBe(1);
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
    [TestMethod]
    public void IsEnqueued_ValidData_ReturnsAccordingToNodeState(NodeLockState lockState, bool expectedResult)
    {
        var agv = Guid.NewGuid();
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [lockState, NodeLockState.Reserved, NodeLockState.Free]);
        if (lockState == NodeLockState.Reserved)
        {
            queue.Nodes.OrderBy(n => n.Position).First().Reserve(agv);
        }
        if (lockState == NodeLockState.Occupied)
        {
            queue.Nodes.OrderBy(n => n.Position).First().Occupy(agv);
        }

        queue.IsEnqueued(agv).ShouldBe(expectedResult);
    }

    [TestMethod]
    public void IsAgvAtQueueHead_DifferentNode_False()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        var agv = queue.Nodes.Single(n => n.Position == 1).OccupiedAgv!.Value;
        queue.IsAgvAtQueueHead(agv).ShouldBeFalse();
    }

    [TestMethod]
    public void IsAgvAtQueueHead_DifferentAgvIsAtHead_False()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        queue.IsAgvAtQueueHead(Guid.NewGuid()).ShouldBeFalse();
    }

    [TestMethod]
    public void IsAgvAtQueueHead_AgvIsAtHead_True()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        var agv = queue.Head!.OccupiedAgv!.Value;
        queue.IsAgvAtQueueHead(agv).ShouldBeTrue();
    }

    [TestMethod]
    public void RemoveAgv_ValidData_ReducesAgvFillQuantity()
    {
        var queue = QueueGenerator.GenerateFullQueue();
        int queueSize = queue.AgvFillQuantity;

        queue.RemoveAgv(queue.Agvs.First());
        queue.AgvFillQuantity.ShouldBe(queueSize - 1);
    }

    [TestMethod]
    public void RemoveAgv_EmptyQueue_DoesNotReduceAgvFillQuantity()
    {
        var queue = QueueGenerator.GenerateEmptyQueue(size: 0);
        int queueSize = queue.AgvFillQuantity;

        queue.RemoveAgv(Guid.NewGuid());
        queue.AgvFillQuantity.ShouldBe(queueSize);
    }

    [TestMethod]
    public void ReachNode_ValidData_NodeOccupied()
    {
        var agv = Guid.NewGuid();

        var queue = QueueGenerator.GenerateEmptyQueue();
        queue.TryEnterQueue();
        queue.TryEnqueue(agv, out Guid? target);

        queue.Nodes.Single(n => n.NodeId == target).ReservedAgv.ShouldBe(agv);

        queue.ReachNode(target!.Value, agv);
        queue.Nodes.Single(n => n.NodeId == target).OccupiedAgv.ShouldBe(agv);
        queue.Nodes.Single(n => n.NodeId == target).ReservedAgv.ShouldBeNull();
    }

    [TestMethod]
    public void ReachNode_UnknownNode_NodeNotOccupied()
    {
        var agv = Guid.NewGuid();

        var queue = QueueGenerator.GenerateEmptyQueue();
        queue.TryEnterQueue();
        queue.TryEnqueue(agv, out Guid? target);

        queue.Nodes.Single(n => n.NodeId == target).ReservedAgv.ShouldBe(agv);

        queue.ReachNode("unknown".ToGuid(), agv);
        queue.Nodes.Any(n => n.OccupiedAgv != null).ShouldBeFalse();
    }

    [TestMethod]
    public void ReachNode_DownStreamNodeReserved_NodeNotOccupied()
    {
        var agv = Guid.NewGuid();
        var agvThatDidNotReachNodeYet = Guid.NewGuid();

        var queue = QueueGenerator.GenerateEmptyQueue();
        queue.TryEnterQueue();
        queue.TryEnterQueue();

        // agv reaches the queue entry node first
        queue.TryEnqueue(agv, out Guid? downstreamNode);
        queue.Nodes.Single(n => n.NodeId == downstreamNode).ReservedAgv.ShouldBe(agv);
        // than agvThatDidNotReachNodeYet reaches the queue entry node
        queue.TryEnqueue(agvThatDidNotReachNodeYet, out Guid? upstreamNode);
        queue.Nodes.Single(n => n.NodeId == upstreamNode).ReservedAgv.ShouldBe(agvThatDidNotReachNodeYet);

        // agv reaches the queue head
        queue.ReachNode(downstreamNode!.Value, agv);
        queue.Nodes.Single(n => n.NodeId == downstreamNode).OccupiedAgv.ShouldBe(agv);
        queue.IsAgvAtQueueHead(agv).ShouldBeTrue();

        // agv gets dequeued
        queue.TryDequeue(out Dictionary<Guid, Guid> agvsToMove);
        queue.Nodes.Any(n => n.GetCurrentAgv() == agv).ShouldBeFalse();
        // and reserves the head for agvThatDidNotReachNodeYet
        agvsToMove[agvThatDidNotReachNodeYet].ShouldBe(downstreamNode.Value);
        queue.Nodes.Single(n => n.NodeId == upstreamNode).ReservedAgv.ShouldBeNull();
        queue.Nodes.Single(n => n.NodeId == downstreamNode).ReservedAgv.ShouldBe(agvThatDidNotReachNodeYet);

        // agvThatDidNotReachNodeYet reaches the original node it was enqueued to
        queue.ReachNode(upstreamNode!.Value, agvThatDidNotReachNodeYet);
        queue.Nodes.Single(n => n.NodeId == upstreamNode).ReservedAgv.ShouldBeNull();
        queue.Nodes.Single(n => n.NodeId == upstreamNode).OccupiedAgv.ShouldBeNull();

        // agvThatDidNotReachNodeYet reaches the queue head
        queue.ReachNode(downstreamNode.Value, agvThatDidNotReachNodeYet);
        queue.Nodes.Single(n => n.NodeId == downstreamNode).OccupiedAgv.ShouldBe(agvThatDidNotReachNodeYet);
        queue.Nodes.Single(n => n.NodeId == downstreamNode).ReservedAgv.ShouldBeNull();
    }

    [TestMethod]
    public void TrySwapAgv_AgvNotInQueue_NoAgvsToMove()
    {
        var queue = QueueGenerator.GenerateEmptyQueue();

        queue.TrySwapAgv(Guid.NewGuid(), out Dictionary<Guid, Guid> agvsToMove);

        agvsToMove.ShouldBeEmpty();
    }

    [TestMethod]
    public void TrySwapAgv_ValidData_FillQuantityUnchanged()
    {
        var queue = QueueGenerator.GenerateFullQueue(nodes: 3);
        var node = queue.Nodes.Single(n => n.Position == 1);
        var agv = Guid.Parse(node.OccupiedAgv!.Value.ToString());

        queue.TrySwapAgv(agv, out Dictionary<Guid, Guid> agvsToMove);

        agvsToMove.ShouldNotBeEmpty();
        agvsToMove.Count.ShouldBe(1);
        queue.IsFull.ShouldBeTrue();
        queue.Agvs.ShouldNotContain(agv);
    }
}