using Gebhardt.Acc.Common.Dictionaries;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;

/// <summary>
/// The factsheet request in-memory cache.
/// </summary>
public class FactsheetRequestCache : ConcurrentDictionaryProvider<Guid, bool>, IFactsheetRequestCacheProvider
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <returns></returns>
    public Task<bool> HasAgvRequestedFactsheet(Guid agvCorrelationId)
    {
        return Task.FromResult(ConcurrentDict.GetValueOrDefault(agvCorrelationId, false));
    }
}
