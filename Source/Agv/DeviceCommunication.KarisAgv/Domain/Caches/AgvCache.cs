using Gebhardt.Acc.Common.Dictionaries;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Caches;

/// <summary>
/// The agv in-memory cache.
/// </summary>
public class AgvCache : ConcurrentDictionaryProvider<Guid, string>, IAgvCacheProvider;
