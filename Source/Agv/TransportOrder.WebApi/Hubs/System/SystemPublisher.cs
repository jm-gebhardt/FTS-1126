using AutoMapper;
using Gebhardt.Acc.Common.WebApi.Hubs;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.LogEntry;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.System;

/// <inheritdoc cref="ISystemPublisher"/>>
public class SystemPublisher(IHubBroadcaster<SystemHub, ISystemClient> hubBroadcaster, IMapper mapper) : ISystemPublisher
{
    /// <inheritdoc cref="ISystemPublisher"/>>
    public Task SendLogEntryCreated(LogEntry logEntry)
    {
        var dto = new LogEntryCreatedDto(null, mapper.Map<LogEntryDto>(logEntry));
        hubBroadcaster.Broadcast(c => c.SystemLogCreated(dto));
        return Task.CompletedTask;
    }
}