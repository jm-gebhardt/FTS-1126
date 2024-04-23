using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Services;

namespace Gebhardt.Acc.Common.Dictionaries;

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

    /// <summary>
    /// Adds to the Concurrent Dictionary and throws if there is an item with that key already.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    void Add(TKey key, TValue value);
}

/// <summary>
/// Base implementation of the concurrent dictionary provider.
/// </summary>
/// <typeparam name="TKey">The key of the dictionary.</typeparam>
/// <typeparam name="TValue">The value of the dictionary.</typeparam>
public class ConcurrentDictionaryProvider<TKey, TValue> : IConcurrentDictionaryProvider<TKey, TValue> where TKey : notnull
{
    /// <summary>
    /// Gets the concurrent dictionary.
    /// </summary>
    public ConcurrentDictionary<TKey, TValue> ConcurrentDict { get; } = new();

    /// <inheritdoc />
    public void Add(TKey key, TValue value)
    {
        ConcurrentDict.AddOrUpdate(key, value, (_, _) => throw new InvalidOperationException("Item already exists"));
    }
}

/// <summary>
/// Provides an interface for a provider of a concurrent dictionary.
/// </summary>
/// <typeparam name="TValue">The value of the concurrent dictionary.</typeparam>
public interface IConcurrentDictionaryProvider<TValue> : IConcurrentDictionaryProvider<Guid, TValue>;

/// <summary>
/// A concurrent dictionary provider for <typeparamref name="TValue"/>.
/// </summary>
/// <typeparam name="TValue">The value of the dictionary.</typeparam>
public class ConcurrentDictionaryProvider<TValue> :ConcurrentDictionaryProvider<Guid, TValue>, IConcurrentDictionaryProvider<TValue>;

/// <summary>
/// Provides a concurrent dictionary of semaphore provider by a Guid key.
/// </summary>
/// <typeparam name="TSemaphore">The type of the semaphore.</typeparam>
public interface IKeyedSemaphoreDictionaryProvider<TSemaphore> : IConcurrentDictionaryProvider<ISemaphoreProvider<TSemaphore>> where TSemaphore : class;