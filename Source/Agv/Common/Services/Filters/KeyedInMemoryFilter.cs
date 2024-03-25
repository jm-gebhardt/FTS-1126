using Gebhardt.StoreWare.Agv.Common.Dictionaries;
using Gebhardt.StoreWare.Agv.Common.Messages;

namespace Gebhardt.StoreWare.Agv.Common.Services.Filters;

/// <summary>
/// A filter that keeps the filter for a given (Guid) key in memory.
/// This is to have e.g. one filter for one CorrelationId (e.g. Agv).
/// </summary>
/// <typeparam name="TObject">The type of the object to filter.</typeparam>
public abstract class KeyedInMemoryFilter<TObject> : KeyedInMemoryFilter<Guid, TObject>
    where TObject : class, IMessageCreatedDate
{
    /// <inheritdoc cref="KeyedInMemoryFilter{TKey,TObject}"/>
    protected KeyedInMemoryFilter(
        ISemaphoreProvider semaphoreProvider,
        IConcurrentDictionaryProvider<Guid, IMessageFilter<TObject>> concurrentDictionaryProvider
    ) : base(semaphoreProvider, concurrentDictionaryProvider)
    {
    }
}

/// <summary>
/// A filter that keeps the filter for a given key in memory.
/// </summary>
/// <typeparam name="TKey">The key to separate the filters.</typeparam>
/// <typeparam name="TObject">The type of the object to filter.</typeparam>
public abstract class KeyedInMemoryFilter<TKey, TObject> : IMessageFilter<TObject>
    where TKey : notnull
    where TObject : class, IMessageCreatedDate
{
    private readonly ISemaphoreProvider _semaphoreProvider;
    private readonly IConcurrentDictionaryProvider<TKey, IMessageFilter<TObject>> _concurrentDictionaryProvider;

    /// <summary>
    /// Return the key of the <typeparamref name="TObject"/> to separate the filters.
    /// </summary>
    /// <param name="value">The value to get the key from.</param>
    /// <returns>The key of the value.</returns>
    protected abstract TKey GetKey(TObject value);

    /// <summary>
    /// Return a new filter object for the given object.
    /// </summary>
    /// <param name="value">The value to create the filter for.</param>
    /// <returns>The new filter object.</returns>
    protected abstract IMessageFilter<TObject> GetFilter(TObject value);

    /// <inheritdoc cref="KeyedInMemoryFilter{TKey,TObject}"/>
    protected KeyedInMemoryFilter(
        ISemaphoreProvider semaphoreProvider,
        IConcurrentDictionaryProvider<TKey, IMessageFilter<TObject>> concurrentDictionaryProvider
    )
    {
        _semaphoreProvider = semaphoreProvider;
        _concurrentDictionaryProvider = concurrentDictionaryProvider;
    }

    /// <inheritdoc />
    public async Task<bool> ShouldActAsync(TObject value)
    {
        return await _semaphoreProvider.ExecuteLockedAsync(async () =>
        {
            var key = GetKey(value);
            if (_concurrentDictionaryProvider.ConcurrentDict.TryGetValue(key, out IMessageFilter<TObject>? existingFilter))
            {
                return await existingFilter.ShouldActAsync(value);
            }

            _concurrentDictionaryProvider.Add(key, GetFilter(value));
            return true;
        });
    }
}