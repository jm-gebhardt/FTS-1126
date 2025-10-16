using AutoMapper;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.System;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Handles system related events.
/// </summary>
[UsedImplicitly]
public class SystemChangedConsumer(ISystemPublisher publisher, IMapper mapper): IConsumer<LogEntryCreated>
{
    /// <inheritdoc cref="SystemChangedConsumer"/>>
    public async Task Consume(ConsumeContext<LogEntryCreated> context)
    {
        var logEntry = context.Message.LogEntry;

        if (logEntry.Category == Categories.System)
        {
            await publisher.SendLogEntryCreated(mapper.Map<LogEntry>(logEntry));
        }
    }
}