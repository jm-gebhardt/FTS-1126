using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using JetBrains.Annotations;
using MassTransit;
using QueueNode = Gebhardt.Acc.Common.Messages.Queue.QueueNode;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles <see cref="Queue"/> related requests.
/// </summary>
[UsedImplicitly]
public class QueueRequestHandler(IQueueRepository queueRepository, IBus bus):
    IConsumer<GetQueues>,
    IConsumer<GetQueue>,
    IConsumer<TryAddQueue>,
    IConsumer<DeleteQueue>
{
    /// <summary>
    /// Handles the <see cref="GetQueues"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetQueues> context)
    {
        ICollection<Queue> queues = await queueRepository.GetQueues();
        await context.RespondAsync(new GotQueues(queues));
    }

    /// <summary>
    /// Handles the <see cref="GetQueue"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetQueue> context)
    {
        var queue = await queueRepository.GetQueueById(context.Message.Id);
        await context.RespondAsync(new GotQueue(queue));
    }

    /// <summary>
    /// Handles the <see cref="TryAddQueue"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TryAddQueue> context)
    {
        (string name, ICollection<QueueNode> queueNodes) = context.Message;
        CheckQueue(name, queueNodes);
        IRequestClient<AddQueue>? requestClient = context.CreateRequestClient<AddQueue>(bus);
        Response<GotQueue> response = await requestClient.GetResponse<GotQueue>(new AddQueue(name, queueNodes));
        await context.RespondAsync(response.Message);
    }

    private void CheckQueue(string name, ICollection<QueueNode> queueNodes)
    {
        var queue = new Queue(name);
        // Mimic adding queue nodes to check consistency (position and node id usage)
        foreach (var node in queueNodes)
        {
            queue.AddNode(node.NodeId, node.Position);
        }
    }

    /// <summary>
    /// Handles the <see cref="DeleteQueue"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<DeleteQueue> context)
    {
        await queueRepository.RemoveQueue(context.Message.Id);
        await context.RespondAsync(new Success());
    }
}