using Gebhardt.Acc.Common.Dictionaries;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;

/// <summary>
/// Provides a fact sheet request in-memory cache.
/// </summary>
public interface IFactSheetRequestCacheProvider: IConcurrentDictionaryProvider<Guid, bool>
{
    /// <summary>
    /// True, if the agv has already requested a fact sheet.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of th agv.</param>
    Task<bool> HasAgvRequestedFactSheet(Guid agvCorrelationId);
}