using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;

/// <summary>
/// Provides a cache for the operating mode of each simulated agv.
/// </summary>
public interface IAgvOperatingModeCache : IConcurrentDictionaryProvider<Guid, AgvOperatingMode>
{
    /// <summary>
    /// Get the current operating mode of the agv.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <returns>The current operating mode.</returns>
    Task<AgvOperatingMode> GetOperatingModeAsync(Guid agvCorrelationId);

    /// <summary>
    /// True, if an agv with the given correlation id exists.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    Task<bool> ExistsAsync(Guid agvCorrelationId);

    /// <summary>
    /// Add or update an agv operation mode.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="mode">The new agv mode.</param>
    Task AddOrUpdateAsync(Guid agvCorrelationId, AgvOperatingMode mode);

    /// <summary>
    /// Remove the operating mode of an agv.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    Task RemoveAsync(Guid agvCorrelationId);
}