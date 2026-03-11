using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;

/// <summary>
/// The agv in-memory cache.
/// </summary>
public class AgvCache : ConcurrentDictionaryProvider<Guid, Agv>, IAgvCacheProvider;