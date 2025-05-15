using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        ICollection<Queue> queues = await fixture.Repository.GetQueues();

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
        var queue = QueueGenerator.GenerateEmptyQueue(nodes:1);

        var fixture = new EfQueueRepositoryTestFixture();
        await fixture.AddRangeAsync(queue.Nodes.Select(q => q.Node));

        await fixture.Repository.AddQueue(queue);
        
        ICollection<Queue> queues = await fixture.Repository.GetQueues();
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
        var emptyQueue = QueueGenerator.GenerateEmptyQueue(name:"empty", nodes: 1, size:0);
        var fullQueue = QueueGenerator.GenerateFullQueue(name: "full", nodes: 1);
        var queueWithCapacity = QueueGenerator.GenerateQueueWithCapacity(name: "capacity", lockStates: [NodeLockState.Occupied, NodeLockState.Free], size:1);

        var fixture = new EfQueueRepositoryTestFixture();
        await fixture.AddRangeAsync(emptyQueue);
        await fixture.AddRangeAsync(fullQueue);
        await fixture.AddRangeAsync(queueWithCapacity);

       ICollection<Queue> queues = await fixture.Repository.GetQueuesWithCapacity();

       queues.Count.ShouldBe(2);
       queues.Select(q => q.Name).ToList().ShouldBe(new List<string>{emptyQueue.Name, queueWithCapacity.Name}, ignoreOrder:true);
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

        ICollection<Queue> queues = await fixture.Repository.GetOccupiedQueues();

        queues.Count.ShouldBe(3);
        queues.Select(q => q.Name).ToList().ShouldBe(new List<string> { fullQueue.Name, queueWithCapacity.Name, queueWithCapacityHeadReserved.Name }, ignoreOrder: true);
        queues.SelectMany(q => q.Nodes).Count().ShouldBe(fullQueue.Nodes.Count + queueWithCapacity.Nodes.Count + queueWithCapacityHeadReserved.Nodes.Count);
    }

    [DataRow(NodeLockState.Free, NodeLockState.Occupied)]
    [DataRow(NodeLockState.Free, NodeLockState.Reserved)]
    [DataTestMethod]
    public async Task GetQueueForAgv_AgvHasNodeInQueue_ReturnsQueue(params NodeLockState[] lockStates)
    {
        var queue = QueueGenerator.GenerateQueueWithCapacity(name: "capacity", lockStates: lockStates);

        var fixture = new EfQueueRepositoryTestFixture();
        await fixture.AddRangeAsync(queue);

        var agv = queue.Nodes.First(n => n.GetCurrentAgv() != null).GetCurrentAgv()!.Value;
        (await fixture.Repository.GetQueueForAgv(agv)).ShouldBeEquivalentTo(queue);
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
}