using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc />
public class EfAgvErrorRepository : IAgvErrorRepository
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;

    /// <inheritdoc cref="EfAgvErrorRepository"/>>
    public EfAgvErrorRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public async Task<(bool changed, List<AgvError> newErrors)> AddOrUpdateAgvErrorStatusAsync(List<AgvError>? errors, Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<AgvError> previousErrors = await context.Set<AgvError>()
            .Where(e => e.AgvCorrelationId == agvCorrelationId && e.ErrorResolved == null)
            .ToListAsync();

        List<AgvError> resolvedErrors = previousErrors.Where(p => errors == null || !errors.Any(p.IsSameError)).ToList();
        ResolveErrors(resolvedErrors);

        List<AgvError> newErrors = new();

        if (errors?.Any() ?? false)
        {
            newErrors = errors.Where(e => !previousErrors.Any(e.IsSameError)).ToList();
            await context.AddRangeAsync(newErrors);
        }
        bool changed = await context.SaveChangesAsync() != 0;
        return (changed, newErrors);
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
        return GetActiveErrorsAsync(new List<Guid> {agvCorrelationId});
    }

    /// <inheritdoc />
    public async Task<IList<AgvError>> GetActiveErrorsAsync(IList<Guid> agvCorrelationIds)
    {
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
        return GetErrorsAsync(new List<Guid> {agvCorrelationId}, dateFrom, dateTo);
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
}