using AutoMapper;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.FilterableParser;
using Gebhardt.Acc.Common.Filter.FilterBuilder;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Filterables;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="ILogEntryRepository"/>>
public class EfLogEntryRepository : ILogEntryRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly FilterableBinder<LogEntry, LogEntryFilterable>.FilterByPaginatedWithOrdering _filter;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="ILogEntryRepository"/>>
    public EfLogEntryRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory, IMapper mapper)
    {
        _dbContextFactory = dbContextFactory;
        _filter = FilterableBinder<LogEntry, LogEntryFilterable>.CreateFilterWithPagination(_dbContextFactory);
        _mapper = mapper;
    }

    /// <inheritdoc cref="ILogEntryRepository"/>>
    public async Task<LogEntry> AddAsync(Categories logCategory, int logCode, string logLevel, Guid? agvCorrelationId, Guid? actionGroupCorrelationId, Guid? stationId, DateTime timestamp, Dictionary<string, object?> variables)
    {
        var logEntry = new LogEntry(logCode, logCategory, logLevel, timestamp, agvCorrelationId, stationId, actionGroupCorrelationId)
        {
            Variables = variables.ToDictionary(
                v => v.Key,
                v => v.Value?.ToString() ?? string.Empty
            )
        };

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.AddAsync(logEntry);
        if (await context.SaveChangesAsync() > 0)
        {
            return logEntry;
        }
        throw new LogEntryNotStoredException(logCode, timestamp);
    }

    /// <inheritdoc cref="ILogEntryRepository"/>>
    public async Task<PagedResult<LogEntry>> FilterAsync(IFilter logEntryFilter, int pageNr, int pageSize)
    {
        PagedResult<LogEntryFilterable> logs = await _filter.Invoke(logEntryFilter, pageNr, pageSize, SortDirection.Desc, [nameof(LogEntry.Timestamp)]);
        return _mapper.Map<PagedResult<LogEntryFilterable>, PagedResult<LogEntry>>(logs);
    }
}