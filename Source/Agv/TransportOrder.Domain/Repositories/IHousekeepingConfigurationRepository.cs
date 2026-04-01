using Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for housekeeping configurations.
/// </summary>
public interface IHousekeepingConfigurationRepository
{
    /// <summary>
    /// Get the current housekeeping configuration.
    /// </summary>
    Task<HousekeepingConfiguration?> GetConfigurationAsync();

    /// <summary>
    /// Add a housekeeping configuration.
    /// </summary>
    Task AddOrUpdateAsync(HousekeepingConfiguration configuration);
}