using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="IMaintenanceLogRepository"/>>
public class EfMaintenanceLogRepository(IDbContextFactory<TransportOrderDbContext> dbContextFactory) : IMaintenanceLogRepository
{
    /// <inheritdoc cref="IMaintenanceLogRepository"/>>
    public async Task AddAsync(MaintenanceLog maintenanceLog)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        await context.AddAsync(maintenanceLog);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc cref="IMaintenanceLogRepository"/>>
    public async Task<IReadOnlyCollection<MaintenanceLog>> GetMaintenancesLogsAsync(ICollection<Guid>? agvIds = null, MaintenanceType? maintenanceType = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        IQueryable<MaintenanceLog> logs = context.Set<MaintenanceLog>().AsNoTracking();

        if (maintenanceType != null)
        {
            logs = logs.Where(l => l.MaintenanceType == maintenanceType);
        }

        if (agvIds == null || agvIds.Count == 0)
        {
            return await logs.OrderByDescending(l => l.ExecutionDate).ToListAsync();
        }

        if (agvIds.Count == 1)
        {
            var agv = agvIds.Single();
            logs = logs.Where(l => l.AgvCorrelationId == agv);
            return await logs.OrderByDescending(l => l.ExecutionDate).ToListAsync();
        }

        logs = logs.Where(l => agvIds.Contains(l.AgvCorrelationId));
        return await logs.OrderByDescending(l => l.ExecutionDate).ToListAsync();
    }

    /// <inheritdoc cref="IMaintenanceLogRepository"/>>
    public async Task<MaintenanceLog?> GetLatestMaintenanceLogForAgvAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<MaintenanceLog>().Where(l => l.AgvCorrelationId == agvCorrelationId).OrderByDescending(l => l.ExecutionDate).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc cref="IMaintenanceLogRepository"/>>
    public async Task<Dictionary<Guid, MaintenanceLog?>> GetLatestMaintenanceLogsForAgvsAsync(ICollection<Guid> agvCorrelationIds, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<MaintenanceLog> latestLogs = await context.Set<MaintenanceLog>()
            .Where(l => agvCorrelationIds.Contains(l.AgvCorrelationId))
            .GroupBy(l => l.AgvCorrelationId)
            .Select(g => g.OrderByDescending(l => l.ExecutionDate).First())
            .ToListAsync(cancellationToken);

        // Create dictionary with all requested AGV IDs, setting null for those without logs
        return agvCorrelationIds.ToDictionary(
            agvId => agvId,
            agvId => latestLogs.FirstOrDefault(log => log.AgvCorrelationId == agvId)
        );
    }
}