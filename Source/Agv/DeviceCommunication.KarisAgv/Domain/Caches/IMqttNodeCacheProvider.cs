using Gebhardt.Acc.Common.Dictionaries;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Caches;

/// <summary>
///  Provides a cache for mqtt nodes.
/// </summary>
public interface IMqttNodeCacheProvider : IConcurrentDictionaryProvider<string, string>;
