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

    /// <summary>
    /// Get the latest maintenance log for an agv, if available.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The maintenance log, if available.</returns>
    Task<MaintenanceLog?> GetLatestMaintenanceLogForAgvAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the latest maintenance log for given agvs, if available.
    /// </summary>
    /// <param name="agvCorrelationIds">The correlation ids of the agvs.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The maintenance log, if available.</returns>
    Task<Dictionary<Guid, MaintenanceLog?>> GetLatestMaintenanceLogsForAgvsAsync(ICollection<Guid> agvCorrelationIds, CancellationToken cancellationToken = default);
}