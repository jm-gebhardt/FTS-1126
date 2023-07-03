using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs.Errors;

/// <inheritdoc />
public class EfAgvErrorRegistry : IAgvErrorRegistry
{
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;

    /// <inheritdoc cref="EfAgvErrorRegistry"/>>
    public EfAgvErrorRegistry(IDbContextFactory<TransportOrderDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public async Task UpdateAgvErrorStatusAsync(List<AgvError>? errors, Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        IQueryable<AgvError> agvErrorsInRegistry = context.Set<AgvError>()
            .Where(e => e.AgvCorrelationId == agvCorrelationId && e.ErrorResolved == null);

        List<AgvError> resolvedErrors = await agvErrorsInRegistry.Where(e => errors == null || !errors.Select(t => t.ErrorType).Contains(e.ErrorType)).ToListAsync();
        ResolveErrors(resolvedErrors);

        if (errors?.Any() ?? false)
        {
            List<AgvError> newErrors = errors.Where(e => !agvErrorsInRegistry.Select(t => t.ErrorType).Contains(e.ErrorType)).ToList();
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
    public async Task<IList<AgvError>> GetActiveErrorsAsync(Guid agvCorrelationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<AgvError> agvErrorsInRegistry = await context.Set<AgvError>()
            .Where(e => e.AgvCorrelationId == agvCorrelationId && e.ErrorResolved == null)
            .OrderByDescending(e => e.ErrorReceived)
            .ToListAsync();
        return agvErrorsInRegistry;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgvError>> GetErrorsAsync(Guid agvCorrelationId, DateTime dateFrom, DateTime dateTo)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        List<AgvError> agvErrorsInRegistry = await context.Set<AgvError>()
            .Where(e => e.AgvCorrelationId == agvCorrelationId && e.ErrorReceived >= dateFrom && e.ErrorReceived <= dateTo)
            .ToListAsync();
        return agvErrorsInRegistry;
    }
}