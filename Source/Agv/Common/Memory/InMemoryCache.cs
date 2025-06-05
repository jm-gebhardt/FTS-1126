using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Gebhardt.Acc.Common.Memory;

/// <inheritdoc />
public class InMemoryCache<TKey, TValue> : IInMemoryCache<TKey, TValue>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, (TValue Value, DateTime CreatedAt)> _cache = new();

    /// <inheritdoc />
    public void AddOrUpdate(TKey key, TValue value)
        => _cache.AddOrUpdate(key,
            _ => (value, DateTime.UtcNow),
            (_, _) => (value, DateTime.UtcNow));

    /// <inheritdoc />
    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            value = entry.Value;
            return true;
        }
        value = default;
        return false;
    }

    /// <inheritdoc />
    public void RemoveOldest(Func<KeyValuePair<TKey, TValue>, bool> predicate)
    {
        KeyValuePair<TKey, (TValue Value, DateTime CreatedAt)> oldest = _cache
            .Where(kvp => predicate(new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value.Value)))
            .OrderBy(kvp => kvp.Value.CreatedAt)
            .FirstOrDefault();

        _cache.TryRemove(oldest.Key, out _);
    }

    /// <inheritdoc />
    public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (_cache.TryRemove(key, out var removedEntry))
        {
            value = removedEntry.Value;
            return true;
        }
        value = default;
        return false;
    }

    /// <inheritdoc />
    public int Count(Func<KeyValuePair<TKey, TValue>, bool> predicate)
        => _cache.Count(kvp => predicate(new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value.Value)));
}