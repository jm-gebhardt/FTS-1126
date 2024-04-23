using AutoMapper;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="AgvBatteryChargeChanged"/> and sends messages via <see cref="IAgvPublisher"/>.
/// </summary>
[PublicAPI]
public class AgvBatteryChargeChangedConsumer :
    IConsumer<AgvBatteryChargeChanged>
{
    private readonly ILogger<AgvBatteryChargeChangedConsumer> _logger;
    private readonly IAgvPublisher _agvPublisher;
    private readonly IMapper _mapper;

    /// <summary>
    /// </summary>
    public AgvBatteryChargeChangedConsumer(
        IAgvPublisher agvPublisher,
        IMapper mapper,
        ILogger<AgvBatteryChargeChangedConsumer> logger)
    {
        _logger = logger;
        _agvPublisher = agvPublisher;
        _mapper = mapper;
    }

    /// <summary>
    /// Send message to all clients
    /// </summary>
    /// <param name="context"></param>
    public Task Consume(ConsumeContext<AgvBatteryChargeChanged> context)
    {
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(AgvBatteryChargeChanged), context.Message.CorrelationId);

        return _agvPublisher.SendAgvBatteryStatusAsync(context.Message);
    }
}