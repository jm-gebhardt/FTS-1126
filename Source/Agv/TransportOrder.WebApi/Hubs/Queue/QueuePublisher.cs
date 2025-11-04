using AutoMapper;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Queue;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Queue;

/// <summary>
/// Publishes Queue related Dtos via <see cref="QueueHub"/>.
/// </summary>
public class QueuePublisher : IQueuePublisher
{
    private readonly IMapper _mapper;
    private readonly IHubBroadcaster<QueueHub, IQueueClient> _hubBroadcaster;

    /// <summary/>
    public QueuePublisher(
        IHubBroadcaster<QueueHub, IQueueClient> hubBroadcaster,
        IMapper mapper
    )
    {
        _hubBroadcaster = hubBroadcaster;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public Task SendQueueChangedAsync(Common.Messages.Queue.Queue queue)
    {
        var dto = _mapper.Map<QueueDto>(queue);
        _hubBroadcaster.Broadcast(c => c.QueueChanged(dto));
        return Task.CompletedTask;
    }
}