
using System.Collections.Concurrent;

namespace TransportOrder.Domain.SemaphoreProvider;

/// <summary>
/// Provides an interface for a provider of a concurrent dictionary.
/// </summary>
/// <typeparam name="TKey">The key of the concurrent dictionary.</typeparam>
/// <typeparam name="TValue">The value of the concurrent dictionary.</typeparam>
public interface IConcurrentDictionaryProvider<TKey, TValue> where TKey : notnull
{
    /// <summary>
    /// Gets the concurrent dictionary.
    /// </summary>
    ConcurrentDictionary<TKey, TValue> ConcurrentDict { get; }
}