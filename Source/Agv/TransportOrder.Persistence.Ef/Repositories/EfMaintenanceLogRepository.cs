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
}