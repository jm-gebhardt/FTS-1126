using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

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
        var queue = QueueGenerator.GenerateEmptyQueue(nodes:3, size:0);
        queue.AgvFillQuantity.ShouldBe(0);
        string entryNode = queue.Nodes.MaxBy(n => n.Position)!.NodeId;
        var fixture = new QueueManagerTestFixture()
            .WithQueues([queue])
            .WithAgv(agv);
       
        (await fixture.QueueManager.TryEnterQueue(agv)).ShouldBe(entryNode);
        fixture.VerifyQueueWasUpdated();
        fixture.VerifyAgvWasUpdated();
        queue.AgvFillQuantity.ShouldBe(1);
    }

    [TestMethod]
    public async Task TryEnqueueAgv_NoQueueForEntryNode_ReturnsNull()
    {
        var agv = Guid.NewGuid();
        var fixture = new QueueManagerTestFixture()
            .WithQueue(null);

        (await fixture.QueueManager.TryEnqueueAgv(agv, "entryNode")).ShouldBeNull();
    }

    [TestMethod]
    public async Task TryEnqueueAgv_AgvIsAlreadyEnqueued_ReturnsNull()
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Occupied, NodeLockState.Reserved], size: 2);
        var entryNode = queue.Nodes.MaxBy(n => n.Position)!;
        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        (await fixture.QueueManager.TryEnqueueAgv(entryNode.State!.AgvCorrelationId.GetValueOrDefault(), entryNode.NodeId)).ShouldBeNull();
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
            .WithQueue(queue);

        (await fixture.QueueManager.TryEnqueueAgv(agv, entryNode.NodeId)).ShouldBe(entryNode.NodeId);

        fixture.VerifyNodeWasOccupied(entryNode.NodeId, agv, true);
    }

    [TestMethod]
    public async Task TryEnqueueAgv_TargetIsNotEntryNode_ReserveNode()
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [NodeLockState.Occupied, NodeLockState.Free, NodeLockState.Free], size: 3);
        var entryNode = queue.Nodes.MaxBy(n => n.Position)!;
        var agv = Guid.NewGuid();
        var nodeToEnqueueTo = queue.Nodes.OrderBy(n => n.Position).First(n => n.State?.NodeState == NodeLockState.Free);

        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        (await fixture.QueueManager.TryEnqueueAgv(agv, entryNode.NodeId)).ShouldBe(nodeToEnqueueTo.NodeId);

        fixture.VerifyNodeWasReserved(nodeToEnqueueTo.NodeId, agv, force: true);
    }

    [TestMethod]
    public async Task TryDequeueAgv_NoQueue_ReturnsNull()
    {
        var agv = Guid.NewGuid();
        var fixture = new QueueManagerTestFixture()
            .WithQueue(null);

        (await fixture.QueueManager.TryDequeueAgv(agv)).ShouldBeNull();
    }

    [TestMethod]
    public async Task TryDequeueAgv_AgvNotAtQueueHead_ReturnsNull()
    {
        var queue = QueueGenerator.GenerateFullQueue(nodes: 2);
        var agv = queue.Nodes.OrderBy(n => n.Position).Last().State!.AgvCorrelationId!.Value;
        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        (await fixture.QueueManager.TryDequeueAgv(agv)).ShouldBeNull();
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
        head.State!.AgvCorrelationId = agv;
        var agvToMove = queue.Nodes.Single(n => n.NodeId != head.NodeId).State!.AgvCorrelationId!.Value;
        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        Dictionary<Guid, string>? agvsToMove = await fixture.QueueManager.TryDequeueAgv(agv);

        agvsToMove.ShouldNotBeEmpty();
        agvsToMove[agvToMove].ShouldBe(head.NodeId);
        fixture.VerifyQueueWasUpdated();
        queue.AgvFillQuantity.ShouldBe(queueSize - 1);
    }

    [TestMethod]
    public async Task TryEscapeQueue_NoQueueForAgv_ReturnsNull()
    {
        var agv = Guid.NewGuid();
        var fixture = new QueueManagerTestFixture()
            .WithQueue(null);

        (await fixture.QueueManager.TryEscapeQueue(agv)).ShouldBeNull();
    }

    [TestMethod]
    public async Task TryEscapeQueue_AgvIsNotInQueue_ReturnsNull()
    {
        var agv = Guid.NewGuid();
        var queue = QueueGenerator.GenerateFullQueue(nodes: 2);
        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        (await fixture.QueueManager.TryEscapeQueue(agv)).ShouldBeNull();
    }

    [TestMethod]
    public async Task TryEscapeQueue_ValidData_ReturnsAgvsToMove()
    {
        const int queueSize = 3;
        var queue = QueueGenerator.GenerateFullQueue(nodes: queueSize);
        queue.AgvFillQuantity.ShouldBe(queueSize);
        var nodeToEscape = queue.Nodes.OrderBy(n => n.Position).Skip(1).Take(1).Single();
        var agv = nodeToEscape.State!.AgvCorrelationId!.Value;
        var agvToMove = queue.Nodes.Single(n => n.Position > nodeToEscape.Position).State!.AgvCorrelationId!.Value;
        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        Dictionary<Guid, string>? agvsToMove = await fixture.QueueManager.TryEscapeQueue(agv);

        agvsToMove.ShouldNotBeEmpty();
        agvsToMove[agvToMove].ShouldBe(nodeToEscape.NodeId);
        fixture.VerifyQueueWasUpdated();
        queue.AgvFillQuantity.ShouldBe(queueSize - 1);
        fixture.VerifyNodeWasReserved(nodeToEscape.NodeId, agvToMove, true);
    }

    [TestMethod]
    public async Task TryReachQueueHead_NoQueueForAgv_AgvNotUpdated()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new QueueManagerTestFixture()
            .WithQueue(null);

        await fixture.QueueManager.TryReachQueueHead(agv, "head node");
        fixture.VerifyAgvWasUpdated(false);
    }

    [TestMethod]
    public async Task TryReachQueueHead_AgvIsNotAtHead_AgvNotUpdated()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsQueueing = true;
        var queue = QueueGenerator.GenerateFullQueue(nodes: 2);
        queue.Nodes.OrderBy(n => n.Position).Last().State!.AgvCorrelationId = agv.CorrelationId;

        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        await fixture.QueueManager.TryReachQueueHead(agv, queue.Head!.NodeId);
        fixture.VerifyAgvWasUpdated(false);
    }

    [TestMethod]
    public async Task TryReachQueueHead_ValidData_AgvUpdated()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        agv.IsQueueing = true;
        var queue = QueueGenerator.GenerateFullQueue(nodes: 2);
        queue.Nodes.OrderBy(n => n.Position).First().State!.AgvCorrelationId = agv.CorrelationId;

        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        await fixture.QueueManager.TryReachQueueHead(agv, queue.Head!.NodeId);
        fixture.VerifyAgvWasUpdated();
        agv.IsQueueing.ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetAgvsAtQueueHeads_ValidData_ReturnsAgvs()
    {
        var emptyQueue = QueueGenerator.GenerateEmptyQueue("empty");
        var fullQueue = QueueGenerator.GenerateFullQueue("full");
        var queueWithCapacity = QueueGenerator.GenerateQueueWithCapacity("capacity", [NodeLockState.Occupied, NodeLockState.Reserved, NodeLockState.Free], size: 2);

        var fixture = new QueueManagerTestFixture()
            .WithQueues([emptyQueue, fullQueue, queueWithCapacity]);

        var expectedAgvs = new List<Guid> { fullQueue.Head!.State!.AgvCorrelationId!.Value, queueWithCapacity.Head!.State!.AgvCorrelationId!.Value };

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
    [DataTestMethod]
    public async Task IsOnAQueueNode_ValidData_ReturnsAccordingToNodes(NodeLockState lockState, bool expectedResult)
    {
        var agv = Guid.NewGuid();
        var queue = QueueGenerator.GenerateQueueWithCapacity(lockStates: [lockState, NodeLockState.Reserved, NodeLockState.Free]);
        if (lockState != NodeLockState.Free)
        {
            queue.Nodes.OrderBy(n => n.Position).First().State!.AgvCorrelationId = agv;
        }

        var fixture = new QueueManagerTestFixture()
            .WithQueue(queue);

        (await fixture.QueueManager.IsOnAQueueNode(agv)).ShouldBe(expectedResult);
    }
}