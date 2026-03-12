using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;

/// <summary>
/// The agv in-memory cache.
/// </summary>
public class AgvCache : ConcurrentDictionaryProvider<Guid, Agv>, IAgvCacheProvider
{
    /// <inheritdoc/>
    public Task<Agv?> GetAgvByCurrentOrderAsync(Guid orderId)
    {
        return Task.FromResult(ConcurrentDict.Values.SingleOrDefault(a => a.CurrentOrder == orderId));
    }
}
