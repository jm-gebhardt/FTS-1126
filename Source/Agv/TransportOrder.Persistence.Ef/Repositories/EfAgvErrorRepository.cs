using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using AgvError = Gebhardt.Acc.Common.Domain.Models.AgvError;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc />
public class EfAgvErrorRepository : IAgvErrorRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;
    private readonly IConcurrentDictionaryProvider<Guid, Lazy<List<AgvError>>> _errorCache;

    /// <inheritdoc cref="EfAgvErrorRepository"/>>
    public EfAgvErrorRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory, IConcurrentDictionaryProvider<Guid, Lazy<List<AgvError>>> errorCache)
    {
        _dbContextFactory = dbContextFactory;
        _errorCache = errorCache;
    }

    /// <inheritdoc />
    public async Task<(bool changed, List<AgvError> newErrors, List<AgvError> resolvedErrors)> AddOrUpdateAgvErrorStatus(List<AgvError>? errors, Guid agvCorrelationId)
    {
        (bool changed, List<AgvError> newErrors, List<AgvError> resolvedErrors) = UpdateErrorCache(errors, agvCorrelationId);

        if (!changed)
        {
            return (changed, newErrors, resolvedErrors);
        }
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        DbSet<AgvError> dbSet = context.Set<AgvError>();
        await dbSet.AddRangeAsync(newErrors);
        dbSet.UpdateRange(resolvedErrors);
        changed = await context.SaveChangesAsync() != 0;
        return (changed, newErrors, resolvedErrors);
    }

    private (bool changed, List<AgvError> newErrors, List<AgvError> resolvedErrors) UpdateErrorCache(List<AgvError>? errors, Guid agvCorrelationId)
    {
        _errorCache.ConcurrentDict.TryGetValue(agvCorrelationId, out Lazy<List<AgvError>>? inMemoryErrors);
        List<AgvError> previousErrors = inMemoryErrors?.Value ?? [];
        List<AgvError> resolvedErrors = previousErrors.Where(p => errors == null || !errors.Any(p.IsSameError)).ToList();
        ResolveErrors(resolvedErrors);
        List<AgvError> newErrors = [];

        if (errors?.Any() ?? false)
        {
            newErrors = errors.Where(e => !previousErrors.Any(e.IsSameError)).ToList();
        }

        _errorCache.ConcurrentDict[agvCorrelationId] = new Lazy<List<AgvError>>(() => [.. newErrors, .. previousErrors.Except(resolvedErrors)]);
        return (newErrors.Count > 0 || resolvedErrors.Count > 0, newErrors, resolvedErrors);
    }

    private static void ResolveErrors(IList<AgvError> resolvedErrors)
    {
        foreach (var resolvedError in resolvedErrors)
        {
            resolvedError.ErrorResolved = DateTime.UtcNow;
        }
    }

    /// <inheritdoc />
    public Task<IList<AgvError>> GetActiveErrorsAsync(Guid agvCorrelationId)
    {
        return GetActiveErrorsAsync(new List<Guid> { agvCorrelationId });
    }

    /// <inheritdoc />
    public async Task<IList<AgvError>> GetActiveErrorsAsync(IList<Guid> agvCorrelationIds)
    {
        if (_errorCache.ConcurrentDict.Keys.Intersect(agvCorrelationIds).Any())
        {
            return _errorCache.ConcurrentDict
                .Where(kvp => agvCorrelationIds.Contains(kvp.Key))
                .SelectMany(kvp => kvp.Value.Value)
                .ToList();
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<AgvError> agvErrorsInRepository = await context.Set<AgvError>()
            .Where(e => agvCorrelationIds.Contains(e.AgvCorrelationId) && e.ErrorResolved == null)
            .OrderByDescending(e => e.ErrorReceived)
            .ToListAsync();
        return agvErrorsInRepository;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgvError>> GetErrorsAsync(Guid agvCorrelationId, DateTime dateFrom, DateTime dateTo)
    {
        return GetErrorsAsync(new List<Guid> { agvCorrelationId }, dateFrom, dateTo);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgvError>> GetErrorsAsync(IList<Guid> agvCorrelationIds, DateTime dateFrom, DateTime dateTo)
    {
        if (_errorCache.ConcurrentDict.Keys.Intersect(agvCorrelationIds).Any())
        {
            return _errorCache.ConcurrentDict.Where(kvp => agvCorrelationIds.Contains(kvp.Key)).SelectMany(kvp => kvp.Value.Value.Where(e => e.ErrorReceived >= dateFrom && e.ErrorReceived <= dateTo)).ToList();
        }
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
        foreach ((var agvCorrelationId, List<AgvError> errors) in await dbContext.Set<AgvError>()
                     .GroupBy(c => c.AgvCorrelationId)
                     .ToDictionaryAsync(q => q.Key, q => q.ToList()))
        {
            _errorCache.ConcurrentDict[agvCorrelationId] = new Lazy<List<AgvError>>(() =>errors);
        }
    }
}