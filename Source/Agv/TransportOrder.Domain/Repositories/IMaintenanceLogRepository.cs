using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for maintenance log related requests.
/// </summary>
public interface IMaintenanceLogRepository
{
    /// <summary>
    /// Add a maintenance log.
    /// </summary>
    /// <param name="maintenanceLog">The maintenance log.</param>
    Task AddAsync(MaintenanceLog maintenanceLog);

    /// <summary>
    /// Get all maintenance logs for given agvs or type.
    /// </summary>
    /// <param name="agvIds">The agv correlation ids.</param>
    /// <param name="maintenanceType">The maintenance type.</param>
    /// <returns>Collection of maintenance logs matching the filter.</returns>
    Task<IReadOnlyCollection<MaintenanceLog>> GetMaintenancesLogsAsync(ICollection<Guid>? agvIds = null, MaintenanceType? maintenanceType = null);
}