using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="AgvStateChanged"/>, <see cref="AgvRemoved" />, <see cref="AgvErrorChanged" />, <see cref="AgvLockedChanged" />, <see cref="AgvOnlineChanged" /> and sends messages via <see cref="IAgvPublisher"/>
/// </summary>
[UsedImplicitly]
public class AgvChangedConsumer : IConsumer<AgvStateChanged>,
    IConsumer<AgvRemoved>,
    IConsumer<AgvErrorChanged>,
    IConsumer<AgvLockedChanged>,
    IConsumer<AgvOnlineChanged>,
    IConsumer<AgvDiscovered>,
    IConsumer<AgvBecameIdle>,
    IConsumer<CargoChanged>
{
    private readonly ILogger<AgvChangedConsumer> _logger;
    private readonly IAgvPublisher _agvPublisher;

    /// <inheritdoc cref="AgvChangedConsumer"/>
    /// <param name="logger"></param>
    /// <param name="agvPublisher"></param>
    public AgvChangedConsumer(
        ILogger<AgvChangedConsumer> logger,
        IAgvPublisher agvPublisher)
    {
        _logger = logger;
        _agvPublisher = agvPublisher;
    }

    /// <summary>
    /// Send AgvDiscovered message to all clients
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<AgvDiscovered> context)
    {
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(AgvDiscovered), context.Message.Agv.CorrelationId);
        await _agvPublisher.SendAgvDiscoveredAsync(context.Message.Agv);
    }

    /// <summary>
    /// Send AgvStateChanged message to all clients
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<AgvStateChanged> context)
    {
        var agv = context.Message.Agv;
        _logger.LogTrace("{MessageType} {CorrelationId} received. It is now: {NewState}", nameof(AgvStateChanged), context.Message.CorrelationId, agv.CurrentState);
        await _agvPublisher.SendAgvStateChangedAsync(agv);
    }

    /// <summary>
    /// Send AgvRemoved message to all clients
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<AgvRemoved> context)
    {
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(AgvRemoved), context.Message.CorrelationId);
        await _agvPublisher.SendAgvRemovedAsync(context.Message.CorrelationId);
    }

    /// <summary>
    /// Send AgvErrorChanged message to all clients
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<AgvErrorChanged> context)
    {
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(AgvErrorChanged), context.Message.CorrelationId);
        await _agvPublisher.SendAgvErrorChangedAsync(context.Message.CorrelationId, context.Message.Errors);
    }

    /// <summary>
    /// Send AgvLockedChanged message to all clients
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Consume(ConsumeContext<AgvLockedChanged> context)
    {
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(AgvLockedChanged), context.Message.CorrelationId);
        await _agvPublisher.SendAgvLockedChangedAsync(context.Message.CorrelationId, context.Message.IsNowLocked);
    }

    /// <summary>
    /// Send AgvOnlineChanged message to all clients
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<AgvOnlineChanged> context)
    {
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(AgvOnlineChanged), context.Message.CorrelationId);
        await _agvPublisher.SendAgvOnlineChangedAsync(context.Message.CorrelationId, context.Message.IsNowOnline);
    }

    /// <summary>
    /// Send SendAgvRouteChanged with empty remaining route.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvBecameIdle> context)
    {
        await _agvPublisher.SendAgvRouteChangedAsync(new CurrentAgvRouteReadModel(context.Message.CorrelationId,
            Array.Empty<string>()));
    }

    /// <summary>
    /// Send Cargo changed message to all clients
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<CargoChanged> context)
    {
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(CargoChanged), context.Message.CorrelationId);
        await _agvPublisher.SendCargoChangeAsync(context.Message.Cargos);
    }
}