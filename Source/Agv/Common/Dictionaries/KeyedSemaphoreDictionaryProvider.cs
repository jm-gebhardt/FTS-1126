using System.Collections.Concurrent;
using Gebhardt.Acc.Common.Services;

namespace Gebhardt.Acc.Common.Dictionaries;

/// <inheritdoc />
public class KeyedSemaphoreDictionaryProvider<TSemaphore> : IKeyedSemaphoreDictionaryProvider<TSemaphore> where TSemaphore : class
{
    /// <inheritdoc />
    public ConcurrentDictionary<Guid, ISemaphoreProvider<TSemaphore>> ConcurrentDict { get; } = new();

    /// <inheritdoc />
    public void Add(Guid key, ISemaphoreProvider<TSemaphore> value)
    {
        if (ConcurrentDict.TryGetValue(key, out _))
        {
            throw new InvalidOperationException($"Key {key} already exists");
        }

        ConcurrentDict.AddOrUpdate(key, value, (_, _) => throw new InvalidOperationException("Item already exists"));
    }
}