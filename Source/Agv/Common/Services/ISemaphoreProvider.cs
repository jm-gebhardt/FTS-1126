using Gebhardt.Acc.Common.Dictionaries;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Services;

/// <summary>
/// Provides an interface for semaphore services
/// </summary>
[UsedImplicitly]
public interface ISemaphoreProvider<T> : ISemaphoreProvider where T : class;

/// <summary>
/// A Guid version of <see cref="IKeyedSemaphoreProvider{TKeu,TSemaphore}"/>
/// </summary>
/// <typeparam name="TSemaphore"></typeparam>
public interface IKeyedSemaphoreProvider<TSemaphore> : IKeyedSemaphoreProvider<Guid, TSemaphore> where TSemaphore : class;

/// <summary>
/// Provides semaphore provider for each <typeparamref name="TKey"/>.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TSemaphore">The type of the semaphore for each key.</typeparam>
public interface IKeyedSemaphoreProvider<in TKey, TSemaphore> where TSemaphore : class
{
    /// <summary>
    /// Get or add a semaphore provider for the given key.
    /// </summary>
    /// <param name="key">The key to get the semaphore provider for.</param>
    /// <returns>The semaphore provider for the given key.</returns>
    ISemaphoreProvider<TSemaphore> Get(TKey key);
}

/// <summary>
/// Provides an interface for semaphore services
/// </summary>
public interface ISemaphoreProvider
{
    /// <summary>
    /// Executes a given action locked by the semaphore.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<TReturn> ExecuteLockedAsync<TReturn>(Func<Task<TReturn>> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a given action locked by the semaphore.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task ExecuteLockedAsync(Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a given action locked by the semaphore.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<TReturn> ExecuteLockedAsync<TReturn>(Func<TReturn> action, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class SemaphoreProvider : ISemaphoreProvider
{
    /// <summary>
    /// The semaphore used by this provider.
    /// </summary>
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private static CancellationToken GetCancellationToken(CancellationToken cancellationToken)
    {
        return cancellationToken != default ? cancellationToken : new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;
    }

    /// <inheritdoc />
    public async Task<TReturn> ExecuteLockedAsync<TReturn>(Func<Task<TReturn>> action, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(GetCancellationToken(cancellationToken));
            return await action();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task ExecuteLockedAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(GetCancellationToken(cancellationToken));
            await action();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TReturn> ExecuteLockedAsync<TReturn>(Func<TReturn> action, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(GetCancellationToken(cancellationToken));
            return action();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

/// <inheritdoc />
public class SemaphoreProvider<T> : ISemaphoreProvider<T> where T : class
{
    /// <summary>
    /// The semaphore used by this provider.
    /// </summary>
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private static CancellationToken GetCancellationToken(CancellationToken cancellationToken)
    {
        return cancellationToken != default ? cancellationToken : new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;
    }

    /// <inheritdoc />
    public async Task<TReturn> ExecuteLockedAsync<TReturn>(Func<Task<TReturn>> action, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(GetCancellationToken(cancellationToken));
            return await action();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task ExecuteLockedAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(GetCancellationToken(cancellationToken));
            await action();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TReturn> ExecuteLockedAsync<TReturn>(Func<TReturn> action, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(GetCancellationToken(cancellationToken));
            return action();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

/// <inheritdoc />
public class KeyedSemaphoreProvider<TSemaphore>(IConcurrentDictionaryProvider<ISemaphoreProvider<TSemaphore>> concurrentDictionaryProvider)
    : IKeyedSemaphoreProvider<TSemaphore>
    where TSemaphore : class
{
    /// <inheritdoc />
    public ISemaphoreProvider<TSemaphore> Get(Guid key) => concurrentDictionaryProvider.ConcurrentDict.GetOrAdd(key, _ => new SemaphoreProvider<TSemaphore>());
}