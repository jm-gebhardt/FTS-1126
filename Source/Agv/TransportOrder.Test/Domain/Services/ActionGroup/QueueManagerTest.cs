using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;
using AgvAction = Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup.AgvAction;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

[TestClass]
public class QueueManagerTest
{
    [TestMethod]
    public async Task TryEnterQueue_NoQueueWithCapacity_ReturnsNull()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new QueueManagerTestFixture()
            .WithQueues([]);

        (await fixture.QueueManager.TryEnterQueue(agv)).ShouldBeNull();
        fixture.VerifyAgvWasUpdated(false);
    }

    [TestMethod]
    public async Task TryEnterQueue_ValidData_ReturnsEntryNodeId()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var queue = QueueGenerator.GenerateEmptyQueue(nodes: 3, size: 0);
        queue.AgvFillQuantity.ShouldBe(0);
        var entryNode = queue.Nodes.MaxBy(n => n.Position)!.NodeId;
        var fixture = new QueueManagerTestFixture()
            .WithQueues([queue])
            .WithAgv(agv);

        (await fixture.QueueManager.TryEnterQueue(agv)).ShouldBe(entryNode);
        fixture.VerifyQueueWasUpdated();
        queue.AgvFillQuantity.ShouldBe(1);
    }

    [TestMethod]
    public async Task TryEnqueueAgv_NoQueueForEntryNode_ReturnsNull()
    {
        var agv = Guid.NewGuid();
        var fixture = new QueueManagerTestFixture()
            .WithQueue(null);

        (await fixture.QueueManager.TryEnqueueAgv(agv, "entryNode".ToGuid())).ShouldBeNull();
    }

    [TestMethod]
    public async Task TryEnqueueAgv_AgvIsAlreadyEnqueued_ReturnsNull()
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Occupied, NodeLockState.Reserved], size: 2);
        var entryNode = queue.Nodes.MaxBy(n => n.Position)!;
        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        (await fixture.QueueManager.TryEnqueueAgv(entryNode.GetCurrentAgv()!.Value, entryNode.NodeId)).ShouldBeNull();
    }

    [TestMethod]
    public async Task TryEnqueueAgv_QueueIsFullWithOtherAgvs_ReturnsNull()
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Occupied, NodeLockState.Reserved], size: 2);
        var entryNode = queue.Nodes.MaxBy(n => n.Position)!;
        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        (await fixture.QueueManager.TryEnqueueAgv(Guid.NewGuid(), entryNode.NodeId)).ShouldBeNull();
    }

    [TestMethod]
    public async Task TryEnqueueAgv_TargetIsEntryNode_OccupyNode()
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Occupied, NodeLockState.Reserved, NodeLockState.Free], size: 3);
        var entryNode = queue.Nodes.MaxBy(n => n.Position)!;
        var agv = Guid.NewGuid();

        var fixture = new QueueManagerTestFixture()
            .WithQueue(null)
            .WithQueueByEntryNode(queue);

        (await fixture.QueueManager.TryEnqueueAgv(agv, entryNode.NodeId)).ShouldBe(entryNode.NodeId);

        queue.Nodes.Single(n => n.NodeId == entryNode.NodeId).OccupiedAgv.ShouldBe(agv);
    }

    [TestMethod]
    public async Task TryEnqueueAgv_TargetIsNotEntryNode_ReserveNode()
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Occupied, NodeLockState.Free, NodeLockState.Free], size: 3);
        var entryNode = queue.Nodes.MaxBy(n => n.Position)!;
        var agv = Guid.NewGuid();
        var nodeToEnqueueTo = queue.Nodes.OrderBy(n => n.Position).First(n => n.GetCurrentAgv() == null);

        var fixture = new QueueManagerTestFixture()
            .WithQueue(null)
            .WithQueueByEntryNode(queue);

        (await fixture.QueueManager.TryEnqueueAgv(agv, entryNode.NodeId)).ShouldBe(nodeToEnqueueTo.NodeId);

        queue.Nodes.Single(n => n.NodeId == nodeToEnqueueTo.NodeId).ReservedAgv.ShouldBe(agv);
    }

    [TestMethod]
    public async Task TryDequeueAgv_NoQueue_ReturnsNull()
    {
        var agv = Guid.NewGuid();
        var fixture = new QueueManagerTestFixture()
            .WithQueue(null);

        (await fixture.QueueManager.TryDequeueAgv(agv, null)).ShouldBeNull();
    }

    [TestMethod]
    public async Task TryDequeueAgv_AgvNotAtQueueHead_ReturnsNull()
    {
        var queue = QueueGenerator.GenerateFullQueue(nodes: 2);
        var agv = queue.Nodes.OrderBy(n => n.Position).Last().OccupiedAgv!.Value;
        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        (await fixture.QueueManager.TryDequeueAgv(agv, null)).ShouldBeNull();
        fixture.VerifyQueueWasUpdated(false);
    }

    [TestMethod]
    public async Task TryDequeueAgv_ValidData_ReturnsAgvsToMove()
    {
        const int queueSize = 2;
        var agv = Guid.NewGuid();
        var queue = QueueGenerator.GenerateFullQueue(nodes: queueSize);
        queue.AgvFillQuantity.ShouldBe(queueSize);
        var head = queue.Head!;
        head.Occupy(agv);
        var agvToMove = queue.Nodes.Single(n => n.NodeId != head.NodeId).GetCurrentAgv()!.Value;
        var agvInQueue = AgvGenerator.GenerateRawAgv(agvToMove, "serial number");
        agvInQueue.IsQueueing = true;
        var fixture = new QueueManagerTestFixture()
            .WithAgv(agvInQueue)
            .WithQueue(queue);

        Dictionary<Guid, Guid>? agvsToMove = await fixture.QueueManager.TryDequeueAgv(agv, head.NodeId);

        agvsToMove.ShouldNotBeEmpty();
        agvsToMove[agvToMove].ShouldBe(head.NodeId);
        fixture.VerifyQueueWasUpdated();
        queue.AgvFillQuantity.ShouldBe(queueSize - 1);
    }

    [TestMethod]
    public async Task TryEscapeQueue_NoQueueForAgv_ReturnsNull()
    {
        var agv = AgvGenerator.GenerateBusyAgv();
        var fixture = new QueueManagerTestFixture()
            .WithAgv(agv)
            .WithQueue(null);

        (await fixture.QueueManager.TryEscapeQueue(agv.CorrelationId, "lastNodeId".ToGuid())).agvsToMove.ShouldBeNull();
    }

    [TestMethod]
    public async Task TryEscapeQueue_AgvIsNotInQueue_ReturnsNull()
    {
        var agv = Guid.NewGuid();
        var queue = QueueGenerator.GenerateFullQueue(nodes: 2);
        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        (await fixture.QueueManager.TryEscapeQueue(agv, "LastNodeId".ToGuid())).agvsToMove.ShouldBeNull();
    }

    [TestMethod]
    public async Task TryEscapeQueue_ValidData_ReturnsAgvsToMove()
    {
        const int queueSize = 3;
        var queue = QueueGenerator.GenerateFullQueue(nodes: queueSize);
        queue.AgvFillQuantity.ShouldBe(queueSize);
        var nodeToEscape = queue.Nodes.OrderBy(n => n.Position).Skip(1).Take(1).Single();
        var agv = nodeToEscape.OccupiedAgv!.Value;
        var agvToMove = queue.Nodes.Single(n => n.Position > nodeToEscape.Position).GetCurrentAgv()!.Value;
        var agvInQueue = AgvGenerator.GenerateRawAgv(agvToMove, "serial number");
        agvInQueue.IsQueueing = true;
        var fixture = new QueueManagerTestFixture()
            .WithAgv(agvInQueue)
            .WithQueue(queue);

        (Dictionary<Guid, Guid>? agvsToMove, int? queueSizeAfterEscaping) = await fixture.QueueManager.TryEscapeQueue(agv, nodeToEscape.NodeId);

        agvsToMove.ShouldNotBeEmpty();
        agvsToMove[agvToMove].ShouldBe(nodeToEscape.NodeId);
        fixture.VerifyQueueWasUpdated();
        queueSizeAfterEscaping.ShouldBe(queueSize - 1);
        nodeToEscape.ReservedAgv.ShouldBe(agvToMove);
    }

    [TestMethod]
    public async Task TryEscapeQueue_AgvOnlyEntered_ReturnsNoAgvsToMoveAndUpdatedQueueSize()
    {
        const int queueSize = 3;
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Reserved, NodeLockState.Reserved, NodeLockState.Free], size: queueSize);
        queue.AgvFillQuantity.ShouldBe(queueSize);
        var nodeToEscape = queue.Nodes.Single(n => n.NodeId == queue.EntryNode);
        var agv = AgvGenerator.GenerateBusyAgv();
        agv.IsQueueing = true;
        var fixture = new QueueManagerTestFixture()
            .WithAgv(agv)
            .WithQueue(null)
            .WithQueueByEntryNode(queue);

        (Dictionary<Guid, Guid>? agvsToMove, int? queueSizeAfterEscaping) = await fixture.QueueManager.TryEscapeQueue(agv.CorrelationId, nodeToEscape.NodeId);

        agvsToMove.ShouldBeEmpty();
        fixture.VerifyQueueWasUpdated();
        queueSizeAfterEscaping.ShouldBe(queueSize - 1);
    }

    [TestMethod]
    public async Task TryReachQueueNode_NoQueueForAgv_AgvNotUpdated()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new QueueManagerTestFixture()
            .WithQueue(null);

        await fixture.QueueManager.TryReachQueueNode(agv, "head node".ToGuid());
        fixture.VerifyAgvWasUpdated(false);
    }

    [TestMethod]
    public async Task TryReachQueueNode_AgvIsNotAtHead_AgvNotUpdatedQueueUpdated()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsQueueing = true;
        var queue = QueueGenerator.GenerateFullQueue(nodes: 2);
        queue.Nodes.OrderBy(n => n.Position).Last().Reserve(agv.CorrelationId);

        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        await fixture.QueueManager.TryReachQueueNode(agv, queue.EntryNode!.Value);
        fixture.VerifyAgvWasUpdated(false);
        queue.Nodes.OrderBy(n => n.Position).Last().OccupiedAgv.ShouldBe(agv.CorrelationId);
        agv.IsQueueing.ShouldBeTrue();
    }

    [TestMethod]
    public async Task TryReachQueueNode_ValidData_AgvUpdated()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsQueueing = true;
        var queue = QueueGenerator.GenerateFullQueue(nodes: 2);
        queue.Nodes.OrderBy(n => n.Position).First().Occupy(agv.CorrelationId);

        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        (await fixture.QueueManager.TryReachQueueNode(agv, queue.Head!.NodeId)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetAgvsAtQueueHeads_ValidData_ReturnsAgvs()
    {
        var emptyQueue = QueueGenerator.GenerateEmptyQueue("empty");
        var fullQueue = QueueGenerator.GenerateFullQueue("full");
        var queueWithCapacity = QueueGenerator.GenerateQueueWithCapacity("capacity", [NodeLockState.Occupied, NodeLockState.Reserved, NodeLockState.Free], size: 2);

        var fixture = new QueueManagerTestFixture()
            .WithQueues([emptyQueue, fullQueue, queueWithCapacity]);

        var expectedAgvs = new List<Guid> { fullQueue.Head!.OccupiedAgv!.Value, queueWithCapacity.Head!.OccupiedAgv!.Value };

        (await fixture.QueueManager.GetAgvsAtQueueHeads()).ShouldBeEquivalentTo(expectedAgvs);
    }

    [TestMethod]
    public async Task IsOnAQueueNode_NoQueueForAgv_ReturnsFalse()
    {
        var agv = Guid.NewGuid();
        var fixture = new QueueManagerTestFixture()
            .WithQueue(null);

        (await fixture.QueueManager.IsOnAQueueNode(agv)).ShouldBeFalse();
    }

    [DataRow(NodeLockState.Occupied, true)]
    [DataRow(NodeLockState.Reserved, false)]
    [DataRow(NodeLockState.Free, false)]
    [TestMethod]
    public async Task IsOnAQueueNode_ValidData_ReturnsAccordingToNodes(NodeLockState lockState, bool expectedResult)
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

        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        (await fixture.QueueManager.IsOnAQueueNode(agv)).ShouldBe(expectedResult);
    }

    [TestMethod]
    public async Task TrySwapAgv_ValidData_ReturnsAgvsToMove()
    {
        var queue = QueueGenerator.GenerateFullQueue(nodes: 3);
        var node = queue.Nodes.Single(n => n.Position == 1);
        var agv = Guid.Parse(node.OccupiedAgv!.Value.ToString());
        var agvToMove = queue.Nodes.Single(n => n.Position > node.Position).GetCurrentAgv()!.Value;
        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);
        var agvInQueue = AgvGenerator.GenerateRawAgv(agv, "serial number");
        agvInQueue.IsQueueing = true;
        (Dictionary<Guid, Guid>? agvsToMove, Guid? entryNode, bool swapped) = await fixture.QueueManager.TrySwapAgv(agvInQueue, AgvGenerator.GenerateIdleAgv());

        agvsToMove.ShouldNotBeEmpty();
        agvsToMove[agvToMove].ShouldBe(node.NodeId);
        fixture.VerifyQueueWasUpdated();
        node.ReservedAgv.ShouldBe(agvToMove);
        queue.Agvs.ShouldNotContain(agv);
        swapped.ShouldBeTrue();
        entryNode.ShouldBe(queue.EntryNode);
    }

    [TestMethod]
    public async Task TrySwapAgv_AgvNotInsideQueue_DifferentQueueAvailable_ReturnsEntryNode()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var queue = QueueGenerator.GenerateEmptyQueue();
        var fixture = new QueueManagerTestFixture()
            .WithQueues([queue])
            .WithQueue(null)
            .WithAgv(agv);
        (Dictionary<Guid, Guid>? agvsToMove, Guid? entryNode, bool swapped) = await fixture.QueueManager.TrySwapAgv(AgvGenerator.GenerateIdleAgv(), agv);

        agvsToMove.ShouldBeNull();
        swapped.ShouldBeTrue();
        entryNode.ShouldBe(queue.EntryNode);
    }

    [TestMethod]
    public async Task TrySwapAgv_AgvNotInsideQueue_NoDifferentQueueAvailable_ReturnsNotSwapped()
    {
        var fixture = new QueueManagerTestFixture()
            .WithQueues([])
            .WithQueue(null);
        (Dictionary<Guid, Guid>? agvsToMove, Guid? entryNode, bool swapped) = await fixture.QueueManager.TrySwapAgv(AgvGenerator.GenerateIdleAgv(), AgvGenerator.GenerateIdleAgv());

        agvsToMove.ShouldBeNull();
        swapped.ShouldBeFalse();
        entryNode.ShouldBeNull();
    }

    [TestMethod]
    public async Task GetQueueEvacuationMoves_ValidData_ReturnsCorrelations()
    {
        var agv = Guid.NewGuid();
        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agv);
        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agv);
        var evacuationMove = new AgvAction() { ActionType = ActionType.Move, ParentId = Guid.NewGuid(), CorrelationId = Guid.NewGuid(), Target = new ActionTarget("evacuation node".ToGraphNode()) };
        var evacuationActionGroup = new TransportOrder.Domain.Models.ActionGroup.ActionGroup { CorrelationId = evacuationMove.ParentId, AssignedAgv = agv, Actions = [evacuationMove], OrderId = Queue.EvacuationOrderId };

        var fixture = new QueueManagerTestFixture()
            .WithActionGroups([pickDropActionGroup, chargeActionGroup, evacuationActionGroup]);

        (await fixture.QueueManager.GetQueueEvacuationMoves(agv)).Single().ShouldBe(evacuationActionGroup.CorrelationId);
    }

    [TestMethod]
    public async Task GetQueueInternalActionGroups_ValidData_ReturnsCorrelations()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agv = new Agv(agvCorrelationId, Guid.NewGuid(), "")
        {
            IsQueueing = true
        };
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Occupied, NodeLockState.Reserved], size: 2);

        var pickDropActionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: agvCorrelationId);
        var chargeActionGroup = AgvActionGenerator.GenerateChargeActionGroup(assignedAgv: agvCorrelationId);
        var queueMove = new AgvAction() { ActionType = ActionType.Move, ParentId = Guid.NewGuid(), CorrelationId = Guid.NewGuid(), Target = new ActionTarget("queue node".ToGraphNode()) };
        var queueActionGroup = new TransportOrder.Domain.Models.ActionGroup.ActionGroup { CorrelationId = queueMove.ParentId, AssignedAgv = agvCorrelationId, Actions = [queueMove], OrderId = "Queuing" };

        var fixture = new QueueManagerTestFixture()
            .WithActionGroups([pickDropActionGroup, chargeActionGroup, queueActionGroup])
            .WithQueue(queue);

        (await fixture.QueueManager.GetQueueInternalActionGroups(agv)).Single().ShouldBe(queueActionGroup.CorrelationId);
    }
}