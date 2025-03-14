using Gebhardt.Acc.Common.Dictionaries;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Caches;

/// <summary>
/// Provides a cache for mqtt edge mappings.
/// </summary>
[UsedImplicitly]
public interface IMqttEdgeMappingCacheProvider : IConcurrentDictionaryProvider<Guid, string>;
