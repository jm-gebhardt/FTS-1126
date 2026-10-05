using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Queue;

[TestClass]
[DoNotParallelize]
public class QueueRequestHandlerTest
{
    [TestMethod]
    public async Task GetQueues_ValidData_RespondsGotQueues()
    {
        var queue = new TransportOrder.Domain.Models.Queue.Queue("a queue");
        var fixture = new QueueRequestHandlerTestFixture<GetQueues>()
            .WithQueue(queue)
            .Build();

        var response = await fixture.GetResponseAsync<GotQueues>(new GetQueues());

        response.Message.Queues.Count.ShouldBe(1);
        response.Message.Queues.First().ShouldBeEquivalentTo(queue);
    }

    [TestMethod]
    public async Task GetQueue_ValidData_RespondsGotQueue()
    {
        var queue = new TransportOrder.Domain.Models.Queue.Queue("a queue");
        var fixture = new QueueRequestHandlerTestFixture<GetQueue>()
            .WithQueue(queue)
            .Build();
        var response = await fixture.GetResponseAsync<GotQueue>(new GetQueue(queue.Id));

        response.Message.Queue.ShouldBeEquivalentTo(queue);
    }

    [TestMethod]
    public async Task DeleteQueue_ValidData_RespondsSuccess()
    {
        var queue = new TransportOrder.Domain.Models.Queue.Queue("a queue");
        var fixture = new QueueRequestHandlerTestFixture<DeleteQueue>()
            .WithQueue(queue)
            .Build();
        var response = await fixture.GetResponseAsync<Success>(new DeleteQueue(queue.Id));
        response.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task TryAddQueue_ValidData_RespondsGotQueue()
    {
        var queue = new TransportOrder.Domain.Models.Queue.Queue("a queue");
        var node = "node name".ToBaseNode();
        queue.AddNode(node.Id, 0);
        var fixture = new QueueRequestHandlerTestFixture<TryAddQueue>()
            .WithQueue(queue)
            .Build();
        var response = await fixture.GetResponseAsync<GotQueue>(new TryAddQueue(queue.Name, queue.Nodes.Select(n => new QueueNode(node, n.Position, null, null)).ToList()));

        response.Message.Queue.ShouldBeEquivalentTo(queue);
    }

    [TestMethod]
    public async Task TryAddQueue_DuplicatedPosition_ThrowsQueueNodeExistsException()
    {
        var fixture = new QueueRequestHandlerTestFixture<TryAddQueue>()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<QueueNodeExistsException>(async () =>
            await fixture.GetResponseAsync<GotQueue>(new TryAddQueue("queue name", [new QueueNode("node 1".ToBaseNode(), 0, null, null), new QueueNode("node 2".ToGraphNode(), 0, null, null)])));
    }

    [TestMethod]
    public async Task TryAddQueue_DuplicatedNodeId_ThrowsQueueNodeExistsException()
    {
        var fixture = new QueueRequestHandlerTestFixture<TryAddQueue>()
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<QueueNodeExistsException>(async () =>
            await fixture.GetResponseAsync<GotQueue>(new TryAddQueue("queue name", [new QueueNode("node id".ToBaseNode(), 0, null, null), new QueueNode("node id".ToGraphNode(), 1, null, null)])));
    }

    [TestMethod]
    public async Task Consume_RemoveAgvFromQueue_ShouldPublishRemoveAgvFromQueue()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new QueueRequestHandlerTestFixture<RemoveAgvFromQueue>()
            .WithAgvIsInQueue(agvCorrelationId)
            .Build();

        await fixture.GetResponseAsync<Success>(new RemoveAgvFromQueue(agvCorrelationId));
        var message = await fixture.GetFirstPublishedMessageAsync<RemoveAgvFromQueue>();
        message.CorrelationId.ShouldBe(agvCorrelationId);
        message.AlsoRemoveFromConsideration.ShouldBeFalse();
    }
}