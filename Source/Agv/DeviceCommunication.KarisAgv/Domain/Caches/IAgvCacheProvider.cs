using Gebhardt.Acc.Common.Dictionaries;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Caches;

/// <summary>
/// Provides an agv cache.
/// </summary>
public interface IAgvCacheProvider : IConcurrentDictionaryProvider<Guid, string>;
