using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Events.Queue;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Queue;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="QueueChanged"/> and sends messages via <see cref="IQueuePublisher"/>
/// </summary>
[UsedImplicitly]
public class QueueChangedConsumer : IConsumer<QueueChanged>
{
    private readonly ILogger<QueueChangedConsumer> _logger;
    private readonly IQueuePublisher _publisher;

    /// <inheritdoc cref="QueueChangedConsumer"/>
    public QueueChangedConsumer(
        ILogger<QueueChangedConsumer> logger,
        IQueuePublisher publisher,
        IMapper mapper)
    {
        _logger = logger;
        _publisher = publisher;
    }

    /// <summary>
    /// Send QueueChanged message to all clients
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<QueueChanged> context)
    {
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(QueueChanged), context.Message.Queue.CorrelationId);
        await _publisher.SendQueueChangedAsync(context.Message.Queue);
    }
}