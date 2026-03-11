using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;

/// <summary>
/// Provides an agv cache.
/// </summary>
public interface IAgvCacheProvider: IConcurrentDictionaryProvider<Guid, Agv>;