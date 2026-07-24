using Gebhardt.Acc.Common.Filter.Api;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.LogEntry;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles log entry related requests.
/// </summary>
[UsedImplicitly]
public class LogEntryRequestHandler(ILogEntryRepository logEntryRepository):
    IConsumer<GetFilteredLogEntries>
{
    /// <inheritdoc cref="LogEntryRequestHandler"/>>
    public async Task Consume(ConsumeContext<GetFilteredLogEntries> context)
    {
        (var filter, int pageNr, int pageSize, var cancellationToken) = context.Message;

        PagedResult<LogEntry> logEntries = await logEntryRepository.FilterAsync(filter.Translate<LogEntry, LogEntryFilterable>(), pageNr, pageSize, cancellationToken);
        await context.RespondAsync(new GotLogEntries(logEntries));
    }
}