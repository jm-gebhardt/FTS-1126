using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

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
    public async Task AddOrUpdateAgvErrorStatusAsync(List<AgvError>? errors, Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<AgvError> agvErrorsInRepository = context.Set<AgvError>()
            .Where(e => e.AgvCorrelationId == agvCorrelationId && e.ErrorResolved == null);

        List<AgvError> resolvedErrors = await agvErrorsInRepository.Where(e => errors == null || !errors.Select(t => t.ErrorType).Contains(e.ErrorType)).ToListAsync();
        ResolveErrors(resolvedErrors);

        if (errors?.Any() ?? false)
        {
            List<AgvError> newErrors = errors.Where(e => !agvErrorsInRepository.Select(t => t.ErrorType).Contains(e.ErrorType)).ToList();
            await context.AddRangeAsync(newErrors);
        }
        await context.SaveChangesAsync();
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
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<AgvError> agvErrorsInRepository = await context.Set<AgvError>()
            .Where(e => agvCorrelationIds.Contains(e.AgvCorrelationId) && e.ErrorReceived >= dateFrom && e.ErrorReceived <= dateTo)
            .ToListAsync();
        return agvErrorsInRepository;
    }
}