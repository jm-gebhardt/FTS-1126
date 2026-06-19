using AutoMapper;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="AgvStateChanged"/>, <see cref="AgvErrorChanged" />, <see cref="AgvLockedChanged" />, <see cref="AgvOnlineChanged" /> and sends messages via <see cref="IAgvPublisher"/>
/// </summary>
[UsedImplicitly]
public class AgvChangedConsumer : IConsumer<AgvStateChanged>,
    IConsumer<AgvErrorChanged>,
    IConsumer<AgvLockedChanged>,
    IConsumer<AgvOnlineChanged>,
    IConsumer<AgvDiscovered>,
    IConsumer<CargoChanged>,
    IConsumer<HasCargoChanged>,
    IConsumer<AgvModeChanged>,
    IConsumer<AgvModeChangePending>,
    IConsumer<AgvZonesChanged>,
    IConsumer<AgvStationsChanged>,
    IConsumer<AgvMaintained>,
    IConsumer<MaintenanceLogCreated>,
    IConsumer<LogEntryCreated>
{
    private readonly ILogger<AgvChangedConsumer> _logger;
    private readonly IAgvPublisher _agvPublisher;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="AgvChangedConsumer"/>
    /// <param name="logger"></param>
    /// <param name="agvPublisher"></param>
    /// <param name="mapper"></param>
    public AgvChangedConsumer(
        ILogger<AgvChangedConsumer> logger,
        IAgvPublisher agvPublisher,
        IMapper mapper
    )
    {
        _logger = logger;
        _agvPublisher = agvPublisher;
        _mapper = mapper;
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
    /// Send Cargo changed message to all clients
    /// </summary>
    public async Task Consume(ConsumeContext<CargoChanged> context)
    {
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(CargoChanged), context.Message.CorrelationId);
        await _agvPublisher.SendCargoChangeAsync(context.Message.Cargos);
    }

    /// <summary>
    /// Handles the <see cref="HasCargoChanged"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<HasCargoChanged> context)
    {
        await _agvPublisher.SendAgvHasCargoChangedAsync(context.Message.AgvCorrelationId, context.Message.HasCargo);
    }

    /// <summary>
    /// Handles the <see cref="AgvModeChanged"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvModeChanged> context)
    {
        var agvModeDto = _mapper.Map<AgvModeDto>(context.Message.Mode);
        await _agvPublisher.SendAgvModeChangedAsync(context.Message.CorrelationId, agvModeDto);
    }

    /// <summary>
    /// Handles the <see cref="AgvModeChangePending"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvModeChangePending> context)
    {
        var agvModeDto = _mapper.Map<AgvModeDto>(context.Message.Mode);
        await _agvPublisher.SendAgvModeChangedAsync(context.Message.CorrelationId, agvModeDto);
    }

    /// <summary>
    /// Handles the <see cref="AgvZonesChanged"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvZonesChanged> context)
    {
        (var correlationId, ICollection<string> zones) = context.Message;
        await _agvPublisher.SendAgvZonesChangedAsync(correlationId, zones);
    }

    /// <summary>
    /// Handles the <see cref="AgvStationsChanged"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvStationsChanged> context)
    {
        (var correlationId, ICollection<string> stations) = context.Message;
        await _agvPublisher.SendAgvStationsChangedAsync(correlationId, stations);
    }

    /// <summary>
    /// Handles the <see cref="AgvMaintained"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvMaintained> context)
    {
        var (correlationId, maintenance) = context.Message;
        await _agvPublisher.SendAgvMaintainedAsync(correlationId, maintenance);
    }

    /// <summary>
    /// Handles the <see cref="MaintenanceLogCreated"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<MaintenanceLogCreated> context)
    {
        var (correlationId, maintenanceLog) = context.Message;
        await _agvPublisher.SendMaintenanceLogCreated(correlationId, maintenanceLog);
    }

    /// <inheritdoc cref="AgvChangedConsumer"/>>
    public async Task Consume(ConsumeContext<LogEntryCreated> context)
    {
        var logEntry = context.Message.LogEntry;

        if (logEntry.Category == Categories.Agv)
        {
            await _agvPublisher.SendLogEntryCreated(logEntry.AgvCorrelationId.GetValueOrDefault(), _mapper.Map<LogEntry>(logEntry));
            return;
        }

        if (logEntry.Category == Categories.Battery)
        {
            await _agvPublisher.SendBatteryLogEntryCreated(logEntry.AgvCorrelationId.GetValueOrDefault(), _mapper.Map<LogEntry>(logEntry));
        }
    }
}