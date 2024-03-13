using System.Collections.Concurrent;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;

/// <inheritdoc />
public class DeviationCacheProvider : IConcurrentDictionaryProvider<string, (double value, DateTime lastUpdated)>
{
    /// <inheritdoc />
    public ConcurrentDictionary<string, (double value, DateTime lastUpdated)> ConcurrentDict { get; } = new();
}