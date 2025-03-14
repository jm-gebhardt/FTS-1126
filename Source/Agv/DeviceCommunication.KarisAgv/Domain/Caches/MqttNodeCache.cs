using Gebhardt.Acc.Common.Dictionaries;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Caches;

/// <inheritdoc cref="IMqttNodeCacheProvider"/>>
public class MqttNodeCache : ConcurrentDictionaryProvider<string, string>, IMqttNodeCacheProvider;
