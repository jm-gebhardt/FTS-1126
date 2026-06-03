using Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for housekeeping configurations.
/// </summary>
public interface IHousekeepingConfigurationRepository
{
    /// <summary>
    /// Get the housekeeping configuration for the given entity type.
    /// </summary>
    Task<HousekeepingConfiguration?> GetConfigurationAsync(HousekeepingEntityType entityType);

    /// <summary>
    /// Add or update the housekeeping configuration for the entity type it belongs to.
    /// </summary>
    Task AddOrUpdateAsync(HousekeepingConfiguration configuration);
}
