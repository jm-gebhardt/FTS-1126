using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="AgvBatteryStatusChanged"/> and sends messages via <see cref="IAgvPublisher"/>.
/// </summary>
[PublicAPI]
public class AgvOperatingDataStatusChangedConsumer :
    IConsumer<AgvOperatingDataStatusChanged>
{
    private readonly ILogger<AgvOperatingDataStatusChangedConsumer> _logger;
    private readonly IAgvPublisher _agvPublisher;
    private readonly IMapper _mapper;

    /// <summary>
    /// </summary>
    public AgvOperatingDataStatusChangedConsumer(
        IAgvPublisher agvPublisher,
        IMapper mapper,
        ILogger<AgvOperatingDataStatusChangedConsumer> logger)
    {
        _logger = logger;
        _agvPublisher = agvPublisher;
        _mapper = mapper;
    }

    /// <summary>
    /// Send message to all clients
    /// </summary>
    /// <param name="context"></param>
    public Task Consume(ConsumeContext<AgvOperatingDataStatusChanged> context)
    {
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(AgvOperatingDataStatusChanged), context.Message.CorrelationId);
        return _agvPublisher.SendAgvOperatingDataStatusAsync(context.Message);
    }
}