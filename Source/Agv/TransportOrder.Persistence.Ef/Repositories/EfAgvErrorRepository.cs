using Gebhardt.Acc.Common.Memory;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AgvError = Gebhardt.Acc.Common.Domain.Models.AgvError;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc />
public class EfAgvErrorRepository : IAgvErrorRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;

    /// <summary>
    /// Contains active errors for AGVs in memory.
    /// </summary>
    private readonly ICache<Guid, List<AgvError>> _errorCache;

    private readonly ILogger<EfAgvErrorRepository> _logger;

    /// <inheritdoc cref="EfAgvErrorRepository"/>>
    public EfAgvErrorRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory,
        ICache<Guid, List<AgvError>> errorCache,
        ILogger<EfAgvErrorRepository> logger)

    {
        _dbContextFactory = dbContextFactory;
        _errorCache = errorCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AddAndUpdateAgvErrorAsync(List<AgvError> newErrors, List<AgvError> resolvedErrors, Guid agvCorrelationId)
    {
        ResolveErrors(resolvedErrors);

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<AgvError> dbSet = context.Set<AgvError>();
        await dbSet.AddRangeAsync(newErrors);
        dbSet.UpdateRange(resolvedErrors);
        bool changed = await context.SaveChangesAsync() != 0;

        if (changed)
        {
            _errorCache.TryGet(agvCorrelationId, out List<AgvError>? previousErrors);
            previousErrors ??= [];
            List<AgvError> newMemory = previousErrors
                .Where(e => resolvedErrors.All(r => r.CorrelationId != e.CorrelationId))
                .Concat(newErrors.Where(n => previousErrors.All(e => e.CorrelationId != n.CorrelationId)))
                .ToList();

            _errorCache.AddOrUpdate(agvCorrelationId, newMemory);
        }
        else
        {
            _logger.LogWarning("No changes were made to the database for AGV {AgvCorrelationId} despite new errors or resolved errors", agvCorrelationId);
        }
    }

    private static void ResolveErrors(IList<AgvError> resolvedErrors)
    {
        foreach (var resolvedError in resolvedErrors)
        {
            resolvedError.ErrorResolved = DateTime.UtcNow;
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgvError>> GetActiveErrorsAsync(Guid agvCorrelationId)
    {
        return GetActiveErrorsAsync(new List<Guid> { agvCorrelationId });
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgvError>> GetActiveErrorsAsync(IList<Guid> agvCorrelationIds)
    {
        return Task.FromResult<IReadOnlyList<AgvError>>(_errorCache.GetValues(agvCorrelationIds).SelectMany(x => x).ToList());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgvError>> GetErrorsAsync(Guid agvCorrelationId, DateTime dateFrom, DateTime dateTo)
    {
        return GetErrorsAsync(new List<Guid> { agvCorrelationId }, dateFrom, dateTo);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgvError>> GetErrorsAsync(IList<Guid> agvCorrelationIds, DateTime dateFrom, DateTime dateTo)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<AgvError> agvErrorsInRepository = await context.Set<AgvError>()
            .Where(e => agvCorrelationIds.Contains(e.AgvCorrelationId) && e.ErrorReceived >= dateFrom && e.ErrorReceived <= dateTo)
            .ToListAsync();
        return agvErrorsInRepository;
    }

    /// <inheritdoc />
    public async Task LoadInMemoryCache()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        List<AgvError> unresolvedErrors = await dbContext.Set<AgvError>()
            .Where(e => e.ErrorResolved == null)
            .ToListAsync();

        Dictionary<Guid, List<AgvError>> errorsByAgv = unresolvedErrors
            .GroupBy(c => c.AgvCorrelationId)
            .ToDictionary(q => q.Key, q => q.ToList());

        foreach ((var agvCorrelationId, List<AgvError> errors) in errorsByAgv)
        {
            _errorCache.AddOrUpdate(agvCorrelationId, errors);
        }
    }
}