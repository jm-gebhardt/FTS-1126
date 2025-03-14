using Gebhardt.Acc.Common.Dictionaries;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Caches;

/// <summary>
/// Provides a cache for mqtt edge mappings.
/// </summary>
public class MqttEdgeMappingCache : ConcurrentDictionaryProvider<Guid, string>, IMqttEdgeMappingCacheProvider;
