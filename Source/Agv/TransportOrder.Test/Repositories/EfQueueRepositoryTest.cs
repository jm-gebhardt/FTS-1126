using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfQueueRepositoryTest
{
    [TestMethod]
    public async Task GetQueues_ValidData_GetsAllQueues()
    {
        var queue = QueueGenerator.GenerateEmptyQueue(nodes: 1);

        var fixture = new EfQueueRepositoryTestFixture();
        fixture.Add(queue);

        var queues = await fixture.Repository.GetQueues();

        queues.Count.ShouldBe(1);

        var returnedQueue = queues.First();
        returnedQueue.Name.ShouldBe(queue.Name);
        returnedQueue.Id.ShouldBe(queue.Id);
        returnedQueue.Nodes.Count.ShouldBe(queue.Nodes.Count);
        var returnedQueueNode = returnedQueue.Nodes.First();
        returnedQueueNode.Node.ShouldBeEquivalentTo(queue.Nodes.First().Node);
        returnedQueueNode.OccupiedAgv.ShouldBeEquivalentTo(queue.Nodes.First().OccupiedAgv);
        returnedQueueNode.Position.ShouldBe(0);
        returnedQueueNode.NodeId.ShouldBe(queue.Nodes.First().NodeId);
    }

    [TestMethod]
    public async Task AddQueue_ValidData_QueueAdded()
    {
        var queue = QueueGenerator.GenerateEmptyQueue(nodes: 1);

        var fixture = new EfQueueRepositoryTestFixture();
        await fixture.AddRangeAsync(queue.Nodes.Select(q => q.Node));

        await fixture.Repository.AddQueue(queue);

        var queues = await fixture.Repository.GetQueues();
        queues.Count.ShouldBe(1);

        var returnedQueue = queues.First();
        returnedQueue.Name.ShouldBe(queue.Name);
        returnedQueue.Id.ShouldBe(queue.Id);
        returnedQueue.Nodes.Count.ShouldBe(queue.Nodes.Count);
        var returnedQueueNode = returnedQueue.Nodes.First();
        returnedQueueNode.Node.ShouldBeEquivalentTo(queue.Nodes.First().Node);
        returnedQueueNode.Position.ShouldBe(0);
        returnedQueueNode.NodeId.ShouldBe(queue.Nodes.First().NodeId);
    }

    [TestMethod]
    public async Task GetQueuesWithCapacity_ValidData_ReturnsNotFullQueues()
    {
        var emptyQueue = QueueGenerator.GenerateEmptyQueue(name: "empty", nodes: 1, size: 0);
        var fullQueue = QueueGenerator.GenerateFullQueue(name: "full", nodes: 1);
        var queueWithCapacity = QueueGenerator.GenerateQueueWithCapacity(name: "capacity", lockStates: [NodeLockState.Occupied, NodeLockState.Free], size: 1);

        var fixture = new EfQueueRepositoryTestFixture();
        await fixture.AddRangeAsync(emptyQueue);
        await fixture.AddRangeAsync(fullQueue);
        await fixture.AddRangeAsync(queueWithCapacity);

        var queues = await fixture.Repository.GetQueuesWithCapacity();

        queues.Count.ShouldBe(2);
        queues.Select(q => q.Name).ToList().ShouldBe(new List<string> { emptyQueue.Name, queueWithCapacity.Name }, ignoreOrder: true);
        queues.SelectMany(q => q.Nodes).Count().ShouldBe(emptyQueue.Nodes.Count + queueWithCapacity.Nodes.Count);
    }

    [TestMethod]
    public async Task GetOccupiedQueues_ValidData_ReturnsQueuesWithOccupiedOrReservedHead()
    {
        var emptyQueue = QueueGenerator.GenerateEmptyQueue(name: "empty", nodes: 1);
        var fullQueue = QueueGenerator.GenerateFullQueue(name: "full", nodes: 1);
        var queueWithCapacity = QueueGenerator.GenerateQueueWithCapacity(name: "capacity", lockStates: [NodeLockState.Occupied, NodeLockState.Free]);
        var queueWithCapacityHeadReserved = QueueGenerator.GenerateQueueWithCapacity(name: "reserved", lockStates: [NodeLockState.Reserved, NodeLockState.Free]);

        var fixture = new EfQueueRepositoryTestFixture();
        await fixture.AddRangeAsync(emptyQueue);
        await fixture.AddRangeAsync(fullQueue);
        await fixture.AddRangeAsync(queueWithCapacity);
        await fixture.AddRangeAsync(queueWithCapacityHeadReserved);

        var queues = await fixture.Repository.GetOccupiedQueues();

        queues.Count.ShouldBe(3);
        queues.Select(q => q.Name).ToList().ShouldBe(new List<string> { fullQueue.Name, queueWithCapacity.Name, queueWithCapacityHeadReserved.Name }, ignoreOrder: true);
        queues.SelectMany(q => q.Nodes).Count().ShouldBe(fullQueue.Nodes.Count + queueWithCapacity.Nodes.Count + queueWithCapacityHeadReserved.Nodes.Count);
    }

    [DataRow(NodeLockState.Free, NodeLockState.Occupied)]
    [DataRow(NodeLockState.Free, NodeLockState.Reserved)]
    [TestMethod]
    public async Task GetQueueForAgv_AgvHasNodeInQueue_ReturnsQueue(params NodeLockState[] lockStates)
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(name: "capacity", lockStates: lockStates);

        var fixture = new EfQueueRepositoryTestFixture();
        await fixture.AddRangeAsync(queue);

        var agv = queue.Nodes.First(n => n.GetCurrentAgv() != null).GetCurrentAgv()!.Value;
        var fromDb = await fixture.Repository.GetQueueForAgv(agv);
        fromDb.ShouldBeEquivalentTo(queue);
    }

    [TestMethod]
    public async Task UpdateQueueSize_ValidData_QueueUpdated()
    {
        var queue = QueueGenerator.GenerateEmptyQueue("queue", 2, 0);
        queue.AgvFillQuantity.ShouldBe(0);
        var fixture = new EfQueueRepositoryTestFixture();
        await fixture.AddAsync(queue);

        queue.TryEnterQueue();
        queue.AgvFillQuantity.ShouldBe(1);

        await fixture.Repository.UpdateQueueSize(queue.Id, queue.AgvFillQuantity);

        var updatedQueue = await fixture.Repository.GetQueueById(queue.Id);
        updatedQueue.AgvFillQuantity.ShouldBe(queue.AgvFillQuantity);
    }

    [TestMethod]
    public async Task GetBlockedQueueNodes_FullQueue_ReturnAllQueueNodes()
    {
        var queue = QueueGenerator.GenerateFullQueue();

        var fixture = new EfQueueRepositoryTestFixture();
        await fixture.AddAsync(queue);

        (await fixture.Repository.GetBlockedQueueNodes()).Count.ShouldBe(queue.Nodes.Count);
    }

    [TestMethod]
    public async Task GetBlockedQueueNodes_EmptyQueue_ReturnNoQueueNodes()
    {
        var queue = QueueGenerator.GenerateEmptyQueue();

        var fixture = new EfQueueRepositoryTestFixture();
        await fixture.AddAsync(queue);

        (await fixture.Repository.GetBlockedQueueNodes()).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetBlockedQueueNodes_AgvHasNotEntered_ReturnEntryNode()
    {
        var queue = QueueGenerator.GenerateEmptyQueue();
        var entryNode = queue.TryEnterQueue()!.Value;

        var fixture = new EfQueueRepositoryTestFixture();
        await fixture.AddAsync(queue);

        (await fixture.Repository.GetBlockedQueueNodes()).Single().ShouldBe(entryNode);
    }

    [TestMethod]
    public async Task GetQueuesOnRoute_ValidData_ReturnsQueues()
    {
        var queueOnRoute = QueueGenerator.GenerateEmptyQueue();
        var queueNotOnRoute = QueueGenerator.GenerateEmptyQueue();

        var fixture = new EfQueueRepositoryTestFixture();
        await fixture.AddRangeAsync(queueNotOnRoute, queueOnRoute);

        var queues = await fixture.Repository.GetQueuesOnRoute(queueOnRoute.NonEntryNodes);
        queues.Count.ShouldBe(1);
        queues.First().Id.ShouldBe(queueOnRoute.Id);
    }
    
    [TestMethod]
    public async Task AddQueue_SameDefinition_ReturnsExistingAndPreservesOccupancyAndReservations()
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(name: "durable", lockStates: [NodeLockState.Occupied, NodeLockState.Reserved]);
        await using var fixture = new EfQueueRepositoryTestFixture();
        await fixture.AddRangeAsync(queue);
        var requested = new Queue(queue.Name);
        foreach (var node in queue.Nodes.Reverse())
        {
            requested.AddNode(node.NodeId, node.Position);
        }

        var existing = await fixture.Repository.AddQueue(requested);

        existing.Id.ShouldBe(queue.Id);
        existing.AgvFillQuantity.ShouldBe(queue.AgvFillQuantity);
        foreach (var node in queue.Nodes)
        {
            var returned = existing.Nodes.Single(n => n.NodeId == node.NodeId);
            returned.OccupiedAgv.ShouldBe(node.OccupiedAgv);
            returned.ReservedAgv.ShouldBe(node.ReservedAgv);
        }
        (await fixture.Repository.GetQueues()).Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task AddQueue_DifferentDefinition_ThrowsConflict()
    {
        var queue = QueueGenerator.GenerateEmptyQueue(name: "durable", nodes: 1);
        await using var fixture = new EfQueueRepositoryTestFixture();
        await fixture.AddRangeAsync(queue);
        var requested = new Queue(queue.Name);
        requested.AddNode(queue.Nodes.Single().NodeId, 5);

        await Should.ThrowAsync<QueueDefinitionConflictException>(() => fixture.Repository.AddQueue(requested));
        (await fixture.Repository.GetQueues()).Single().Id.ShouldBe(queue.Id);
    }

    [TestMethod]
    public async Task AddQueue_MissingGraphNodes_ThrowsSpecificDependencyFailureWithoutInserting()
    {
        var queue = QueueGenerator.GenerateEmptyQueue(nodes: 2);
        await using var fixture = new EfQueueRepositoryTestFixture();

        var exception = await Should.ThrowAsync<QueueNodesNotReadyException>(() => fixture.Repository.AddQueue(queue));

        foreach (var node in queue.Nodes)
        {
            exception.Message.ShouldContain(node.NodeId.ToString());
        }
        (await fixture.Repository.GetQueues()).ShouldBeEmpty();
    }
}