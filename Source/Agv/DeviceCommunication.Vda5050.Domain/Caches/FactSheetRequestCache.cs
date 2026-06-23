using Gebhardt.Acc.Common.Dictionaries;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;

/// <summary>
/// The fact sheet request in-memory cache.
/// </summary>
public class FactSheetRequestCache : ConcurrentDictionaryProvider<Guid, bool>, IFactSheetRequestCacheProvider
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <returns></returns>
    public Task<bool> HasAgvRequestedFactSheet(Guid agvCorrelationId)
    {
        return Task.FromResult(ConcurrentDict.GetValueOrDefault(agvCorrelationId, false));
    }
}
