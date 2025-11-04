using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Queue;

[TestClass]
public class QueueRequestHandlerTest
{
    [TestMethod]
    public async Task GetQueues_ValidData_RespondsGotQueues()
    {
        var queue = new TransportOrder.Domain.Models.Queue.Queue("a queue");
        QueueRequestHandlerTestFixture<GetQueues> fixture = new QueueRequestHandlerTestFixture<GetQueues>()
            .WithQueue(queue)
            .Build();

        Response<GotQueues> response = await fixture.GetResponseAsync<GotQueues>(new GetQueues());

        response.Message.Queues.Count.ShouldBe(1);
        response.Message.Queues.First().ShouldBeEquivalentTo(queue);
    }

    [TestMethod]
    public async Task GetQueue_ValidData_RespondsGotQueue()
    {
        var queue = new TransportOrder.Domain.Models.Queue.Queue("a queue");
        QueueRequestHandlerTestFixture<GetQueue> fixture = new QueueRequestHandlerTestFixture<GetQueue>()
            .WithQueue(queue)
            .Build();
        Response<GotQueue> response = await fixture.GetResponseAsync<GotQueue>(new GetQueue(queue.Id));

        response.Message.Queue.ShouldBeEquivalentTo(queue);
    }

    [TestMethod]
    public async Task DeleteQueue_ValidData_RespondsSuccess()
    {
        var queue = new TransportOrder.Domain.Models.Queue.Queue("a queue");
        QueueRequestHandlerTestFixture<DeleteQueue> fixture = new QueueRequestHandlerTestFixture<DeleteQueue>()
            .WithQueue(queue)
            .Build();
        Response<Success> response = await fixture.GetResponseAsync<Success>(new DeleteQueue(queue.Id));
        response.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task TryAddQueue_ValidData_RespondsGotQueue()
    {
        var queue = new TransportOrder.Domain.Models.Queue.Queue("a queue");
        var node = "node name".ToBaseNode();
        queue.AddNode(node.Id, 0);
        QueueRequestHandlerTestFixture<TryAddQueue> fixture = new QueueRequestHandlerTestFixture<TryAddQueue>()
            .WithQueue(queue)
            .Build();
        Response<GotQueue> response = await fixture.GetResponseAsync<GotQueue>(new TryAddQueue(queue.Name, queue.Nodes.Select(n => new QueueNode(node, n.Position, null, null)).ToList()));

        response.Message.Queue.ShouldBeEquivalentTo(queue);
    }

    [TestMethod]
    public async Task TryAddQueue_DuplicatedPosition_ThrowsQueueNodeExistsException()
    {
        QueueRequestHandlerTestFixture<TryAddQueue> fixture = new QueueRequestHandlerTestFixture<TryAddQueue>()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<QueueNodeExistsException>(async () =>
            await fixture.GetResponseAsync<GotQueue>(new TryAddQueue("queue name", [new QueueNode("node 1".ToBaseNode(), 0, null, null), new QueueNode("node 2".ToGraphNode(), 0, null, null)])));
    }

    [TestMethod]
    public async Task TryAddQueue_DuplicatedNodeId_ThrowsQueueNodeExistsException()
    {
        QueueRequestHandlerTestFixture<TryAddQueue> fixture = new QueueRequestHandlerTestFixture<TryAddQueue>()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<QueueNodeExistsException>(async () =>
            await fixture.GetResponseAsync<GotQueue>(new TryAddQueue("queue name", [new QueueNode("node id".ToBaseNode(), 0, null, null), new QueueNode("node id".ToGraphNode(), 1, null, null)])));
    }
}