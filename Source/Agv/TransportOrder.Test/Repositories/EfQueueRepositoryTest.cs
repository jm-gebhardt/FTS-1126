using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfQueueRepositoryTest
{
    [TestMethod]
    public async Task GetQueues_ValidData_GetsAllQueues()
    {
        var mapper = MappingHelper.GetMapper(new GraphProfile());
        var graphNode = new GraphNode("a node");
        var node = mapper.Map<EfNode>(graphNode);
        var nodeState = new NodeLock(node.Id, NodeLockState.Occupied, false, null, DateTime.UtcNow);
        var queue = new Queue("a queue");
        queue.AddNode(node.Id,0);

        var fixture = new EfQueueRepositoryTestFixture();
        fixture.Add(node);
        fixture.Add(nodeState);
        fixture.Add(queue);

        ICollection<Queue> queues = await fixture.Repository.GetQueues();

        queues.Count.ShouldBe(1);

        var returnedQueue = queues.First();
        returnedQueue.Name.ShouldBe(queue.Name);
        returnedQueue.Id.ShouldBe(queue.Id);
        returnedQueue.Nodes.Count.ShouldBe(queue.Nodes.Count);
        var returnedQueueNode = returnedQueue.Nodes.First();
        returnedQueueNode.Node.ShouldBeEquivalentTo(node);
        returnedQueueNode.State.ShouldBeEquivalentTo(nodeState);
        returnedQueueNode.Position.ShouldBe(0);
        returnedQueueNode.NodeId.ShouldBe(node.Id);
    }

    [TestMethod]
    public async Task AddQueue_ValidData_QueueAdded()
    {
        var mapper = MappingHelper.GetMapper(new GraphProfile());
        var graphNode = new GraphNode("a node");
        var node = mapper.Map<EfNode>(graphNode);
        var nodeState = new NodeLock(node.Id, NodeLockState.Occupied, false, null, DateTime.UtcNow);
        var queue = new Queue("a queue");
        queue.AddNode(node.Id, 0);

        var fixture = new EfQueueRepositoryTestFixture();
        fixture.Add(node);
        fixture.Add(nodeState);

        await fixture.Repository.AddQueue(queue);
        
        ICollection<Queue> queues = await fixture.Repository.GetQueues();
        queues.Count.ShouldBe(1);

        var returnedQueue = queues.First();
        returnedQueue.Name.ShouldBe(queue.Name);
        returnedQueue.Id.ShouldBe(queue.Id);
        returnedQueue.Nodes.Count.ShouldBe(queue.Nodes.Count);
        var returnedQueueNode = returnedQueue.Nodes.First();
        returnedQueueNode.Node.ShouldBeEquivalentTo(node);
        returnedQueueNode.State.ShouldBeEquivalentTo(nodeState);
        returnedQueueNode.Position.ShouldBe(0);
        returnedQueueNode.NodeId.ShouldBe(node.Id);
    }
}