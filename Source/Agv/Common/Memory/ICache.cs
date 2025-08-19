using System.Diagnostics.CodeAnalysis;

namespace Gebhardt.Acc.Common.Memory;

/// <summary>
/// Represents an in-memory caching mechanism with methods to add, update, retrieve,
/// and remove items, along with functionality to track cache size.
/// </summary>
/// <typeparam name="TKey">The type of keys used to identify cached items.</typeparam>
/// <typeparam name="TValue">The type of values stored in the cache.</typeparam>
public interface ICache<TKey, TValue>
{
    /// <summary>
    /// Attempts to retrieve the value associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the cached entry to retrieve.</param>
    /// <param name="value">When the method returns, contains the value associated with the key if found; otherwise, the default value for the type of the value parameter.</param>
    /// <returns>True if the key was found in the cache; otherwise, false.</returns>
    bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value);

    /// <summary>
    /// Retrieves values from the cache that correspond to the specified keys.
    /// </summary>
    /// <param name="keys">The collection of keys to retrieve values for.</param>
    /// <returns>An enumeration of values associated with the provided keys.</returns>
    IEnumerable<TValue> GetValues(IEnumerable<TKey> keys);

    /// <summary>
    /// Counts the number of cache entries that match the specified predicate.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>The number of elements that satisfy the condition.</returns>
    public int Count(Func<KeyValuePair<TKey, TValue>, bool> predicate);

    /// <summary>
    /// Adds a new entry to the cache or updates the value of an existing entry identified by the specified key.
    /// </summary>
    /// <param name="key">The key that identifies the cached entry to be added or updated.</param>
    /// <param name="value">The value to be cached or used to update the existing entry.</param>
    void AddOrUpdate(TKey key, TValue value);

    /// <summary>
    /// Removes the oldest entry from the cache that matches the specified predicate.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition that determines which entries are eligible for removal.</param>
    void RemoveOldest(Func<KeyValuePair<TKey, TValue>, bool> predicate);

    /// <summary>
    /// Attempts to remove the entry identified by the specified key from the cache.
    /// </summary>
    /// <param name="key">The key that identifies the cached entry to be removed.</param>
    /// <param name="value">When the method returns, contains the value associated with the removed key if the removal is successful; otherwise, the default value for the type of the value parameter.</param>
    /// <returns>True if the key was found and the entry was successfully removed from the cache; otherwise, false.</returns>
    bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value);
}