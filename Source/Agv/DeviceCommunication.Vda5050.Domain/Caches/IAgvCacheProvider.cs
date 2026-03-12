using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;

/// <summary>
/// Provides an agv cache.
/// </summary>
public interface IAgvCacheProvider : IConcurrentDictionaryProvider<Guid, Agv>
{
    /// <summary>
    /// Get agv by given order id.
    /// </summary>
    /// <param name="orderId">The current order id of the agv.</param>
    /// <returns>An agv, if any runs the given order.</returns>
    Task<Agv?> GetAgvByCurrentOrderAsync(Guid orderId);
}
