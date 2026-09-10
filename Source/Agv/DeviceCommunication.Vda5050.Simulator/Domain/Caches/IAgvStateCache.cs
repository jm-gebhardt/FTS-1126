using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;

/// <summary>
/// Provides a cache for the latest VDA5050 state of each simulated agv.
/// </summary>
public interface IAgvStateCache : IConcurrentDictionaryProvider<Guid, MqttState>
{
    /// <summary>
    /// Add or update the latest state of an agv.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="state">The latest state.</param>
    Task AddOrUpdateAsync(Guid agvCorrelationId, MqttState state);

    /// <summary>
    /// Get the latest state of an agv, or null if none has been published yet.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <returns>The latest state, or null.</returns>
    Task<MqttState?> TryGetAsync(Guid agvCorrelationId);

    /// <summary>
    /// Remove the latest state of an agv.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    Task RemoveAsync(Guid agvCorrelationId);
}