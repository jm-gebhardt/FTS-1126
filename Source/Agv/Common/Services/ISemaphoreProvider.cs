namespace Gebhardt.StoreWare.Agv.Common.Services;

/// <summary>
/// Provides an interface for semaphore services
/// </summary>
public interface ISemaphoreProvider
{
    /// <summary>
    /// The <see cref="Semaphore"/>.
    /// </summary>
    SemaphoreSlim Semaphore { get; }
}