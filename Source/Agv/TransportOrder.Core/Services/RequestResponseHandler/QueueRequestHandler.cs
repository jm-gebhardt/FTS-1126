using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Queue = Gebhardt.Acc.TransportOrder.Domain.Models.Queue.Queue;
using QueueNode = Gebhardt.Acc.Common.Messages.Queue.QueueNode;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles <see cref="Domain.Models.Queue.Queue"/> related requests.
/// </summary>
[UsedImplicitly]
public class QueueRequestHandler(IQueueRepository queueRepository, IQueueManager queueManager, IBus bus, ILogger<QueueRequestHandler> logger) :
    IConsumer<GetQueues>,
    IConsumer<GetQueue>,
    IConsumer<TryAddQueue>,
    IConsumer<DeleteQueue>,
    IConsumer<RemoveAgvFromQueue>,
    IConsumer<AdjustAgvFillQuantity>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetQueues> context)
    {
        ICollection<Queue> queues = await queueRepository.GetQueues();
        await context.RespondAsync(new GotQueues(queues));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetQueue> context)
    {
        var queue = await queueRepository.GetQueueById(context.Message.Id);
        await context.RespondAsync(new GotQueue(queue));
    }

    /// <inheritdoc />
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
            queue.AddNode(node.Node.Id, node.Position);
        }
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DeleteQueue> context)
    {
        await queueRepository.RemoveQueue(context.Message.Id);
        await context.RespondAsync(new Success());
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<RemoveAgvFromQueue> context)
    {
        var message = context.Message;
        logger.LogInformation("Removing agv {AgvCorrelationId} from queue. QueueId: {QueueId}, QueueNodeId: {QueueNodeId}",
            message.CorrelationId, message.QueueId, message.QueueNodeId);

        if (!await queueManager.IsAgvInQueue(message.CorrelationId))
        {
            throw new AgvNotInQueueException(message.CorrelationId, message.QueueId);
        }

        await bus.Publish(message);

        await context.RespondAsync(new Success());
    }
    
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AdjustAgvFillQuantity> context)
    {
        var message = context.Message;
        logger.LogInformation("Adjust AgvFillQuantity of queue {QueueId}", message.QueueId);
        
        // Check whether the queue exists
        var queue = await queueRepository.GetQueueById(message.QueueId);
        
        await bus.Publish(new AdjustAgvFillQuantity(queue.Id, queue.Name));
    
        await context.RespondAsync(new Success());
    }
}