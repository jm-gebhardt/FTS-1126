using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Services;

/// <summary>
/// Provides an interface for semaphore services
/// </summary>
[UsedImplicitly]
public interface ISemaphoreProvider<T> : ISemaphoreProvider where T : class
{
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
}