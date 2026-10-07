namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;

/// <summary>
/// Provides a cache for the raw JSON factsheet of each agv type.
/// </summary>
public interface IFactsheetCache
{
    /// <summary>
    /// Add or update the factsheet of an agv type.
    /// </summary>
    /// <param name="agvTypeId">The agv type id.</param>
    /// <param name="factsheetJson">The factsheet as raw JSON.</param>
    Task AddOrUpdateAsync(Guid agvTypeId, string factsheetJson);

    /// <summary>
    /// Get the factsheet of an agv type, or null if none is cached.
    /// </summary>
    /// <param name="agvTypeId">The agv type id.</param>
    /// <returns>The factsheet as raw JSON, or null.</returns>
    Task<string?> TryGetAsync(Guid agvTypeId);
}