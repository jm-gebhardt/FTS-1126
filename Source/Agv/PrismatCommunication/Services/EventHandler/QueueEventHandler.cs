using Gebhardt.Acc.Common.Messages.Queue;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Services.EventHandler;

/// <summary>
/// Handles queue-related events
/// </summary>
[UsedImplicitly]
public class QueueEventHandler(KaeserGraphConfiguration configuration) : 
    IConsumer<QueueBecameEmpty>,
    IConsumer<QueueEntered>
{
    /// <summary>
    /// Handles the <see cref="QueueBecameEmpty"/> event.
    /// </summary>
    public Task Consume(ConsumeContext<QueueBecameEmpty> context)
    {
        configuration.SetQueueIsOccupied(false);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the <see cref="QueueEntered"/> event.
    /// </summary>
    public Task Consume(ConsumeContext<QueueEntered> context)
    {
        configuration.SetQueueIsOccupied(true);
        return Task.CompletedTask;
    }
}