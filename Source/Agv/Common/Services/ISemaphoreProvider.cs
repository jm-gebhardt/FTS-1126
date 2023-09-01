namespace Gebhardt.StoreWare.Agv.Common.Services;

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
    Task<T> ExecuteLockedAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken);

    /// <summary>
    /// Executes a given action locked by the semaphore.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task ExecuteLockedAsync(Func<Task> action, CancellationToken cancellationToken);
}

/// <inheritdoc />
public abstract class BaseSemaphoreProvider : ISemaphoreProvider
{
    /// <summary>
    /// The semaphore used by this provider.
    /// </summary>
    protected abstract SemaphoreSlim Semaphore { get; }

    /// <inheritdoc />
    public async Task<T> ExecuteLockedAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        try
        {
            await Semaphore.WaitAsync(cancellationToken);
            return await action();
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task ExecuteLockedAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        try
        {
            await Semaphore.WaitAsync(cancellationToken);
            await action();
        }
        finally
        {
            Semaphore.Release();
        }
    }
}