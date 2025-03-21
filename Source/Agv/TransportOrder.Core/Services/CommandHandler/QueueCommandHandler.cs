using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using QueueNode = Gebhardt.Acc.Common.Messages.Queue.QueueNode;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Handles <see cref="Domain.Models.Queue"/> related commands.
/// </summary>
[UsedImplicitly]
public class QueueCommandHandler(IQueueRepository queueRepository, ILogger<QueueCommandHandler> logger) :
    IConsumer<AddQueue>,
    IConsumer<GetQueueFillQuantities>
{
    /// <summary>
    /// Handles the <see cref="AddQueue"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AddQueue> context)
    {
        (string name, ICollection<QueueNode> queueNodes) = context.Message;

        logger.LogInformation("Adding queue {QueueName}", name);
        var queue = new Queue(name);
        foreach (var node in queueNodes)
        {
            queue.AddNode(node.NodeId, node.Position);
            logger.LogInformation("Adding node {NodeId} to queue {QueueName} at position {QueuePosition}", name, node.NodeId, node.Position);
        }

        await queueRepository.AddQueue(queue);

        await context.RespondAsync(new GotQueue(queue));
    }

    /// <summary>
    /// Handles the <see cref="GetQueueFillQuantities"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetQueueFillQuantities> context)
    {
        ICollection<Queue> queues = await queueRepository.GetQueues();

        List<QueueFillQuantity> fillQuantities = queues.Select(q => new QueueFillQuantity(q.Name, q.AgvFillQuantity, q.Nodes.Count)).ToList();

        await context.RespondAsync(new GotQueueFillQuantities(fillQuantities));
    }
}