using Gebhardt.Acc.Common.Dictionaries;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;

/// <summary>
/// Provides a factsheet request in-memory cache.
/// </summary>
public interface IFactsheetRequestCacheProvider: IConcurrentDictionaryProvider<Guid, bool>
{
    /// <summary>
    /// True, if the agv has already requested a factsheet.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of th agv.</param>
    Task<bool> HasAgvRequestedFactsheet(Guid agvCorrelationId);
}