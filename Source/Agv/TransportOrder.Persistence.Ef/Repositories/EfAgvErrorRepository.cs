using Gebhardt.Acc.Common.Memory;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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

    /// <inheritdoc cref="EfAgvErrorRepository"/>>
    public EfAgvErrorRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory,
        ICache<Guid, List<AgvError>> errorCache)

    {
        _dbContextFactory = dbContextFactory;
        _errorCache = errorCache;
    }

    /// <inheritdoc />
    public async Task AddAndUpdateAgvErrorAsync(List<AgvError> newErrors, List<AgvError> resolvedErrors, Guid agvCorrelationId, CancellationToken cancellationToken = default)
    {
        List<DateTime?> resolvedTimesBeforeWrite = resolvedErrors.Select(e => e.ErrorResolved).ToList();
        ResolveErrors(resolvedErrors);

        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            DbSet<AgvError> dbSet = context.Set<AgvError>();
            await dbSet.AddRangeAsync(newErrors, cancellationToken);
            dbSet.UpdateRange(resolvedErrors);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            RestoreErrors(resolvedErrors, resolvedTimesBeforeWrite);
            throw;
        }

        _errorCache.TryGet(agvCorrelationId, out List<AgvError>? previousErrors);
        previousErrors ??= [];
        List<AgvError> newMemory = previousErrors
            .Where(e => resolvedErrors.All(r => r.CorrelationId != e.CorrelationId))
            .Concat(newErrors.Where(n => previousErrors.All(e => e.CorrelationId != n.CorrelationId)))
            .ToList();

        _errorCache.AddOrUpdate(agvCorrelationId, newMemory);
    }

    private static void ResolveErrors(IList<AgvError> resolvedErrors)
    {
        foreach (var resolvedError in resolvedErrors)
        {
            resolvedError.ErrorResolved = DateTime.UtcNow;
        }
    }

    private static void RestoreErrors(IList<AgvError> resolvedErrors, IReadOnlyList<DateTime?> resolvedTimes)
    {
        for (int i = 0; i < resolvedErrors.Count; i++)
        {
            resolvedErrors[i].ErrorResolved = resolvedTimes[i];
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
    public Task<IReadOnlyList<AgvError>> GetErrorsAsync(Guid agvCorrelationId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default)
    {
        return GetErrorsAsync(new List<Guid> { agvCorrelationId }, dateFrom, dateTo, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgvError>> GetErrorsAsync(IList<Guid> agvCorrelationIds, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        List<AgvError> agvErrorsInRepository = await context.Set<AgvError>()
            .Where(e => agvCorrelationIds.Contains(e.AgvCorrelationId) && e.ErrorReceived >= dateFrom && e.ErrorReceived <= dateTo)
            .ToListAsync(cancellationToken);
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

    /// <inheritdoc />
    public async Task DeleteOutdatedErrors(DateTime outDatedTime)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(30)); // the default command timeout is 30s, which could be too short, depending on the amount of data

        var outdated = new SqlParameter("OutdatedTime", outDatedTime);

        await dbContext.Database.ExecuteSqlRawAsync("EXECUTE [dbo].[HouseKeepAgvErrors] @OutdatedTime", outdated);
    }

    /// <inheritdoc />
    public async Task DeleteOutdatedWifiInformation(DateTime outDatedTime)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(30)); // the default command timeout is 30s, which could be too short, depending on the amount of data

        var outdated = new SqlParameter("OutdatedTime", outDatedTime);

        await dbContext.Database.ExecuteSqlRawAsync("EXECUTE [dbo].[HouseKeepWIFIInformation] @OutdatedTime", outdated);
    }
}