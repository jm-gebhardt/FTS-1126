using Gebhardt.StoreWare.Agv.Common.Services;

namespace TransportOrder.Domain.SemaphoreProvider;

/// <inheritdoc cref="IActionManagerSemaphoreProvider" />
public class ActionManagerSemaphoreProvider : BaseSemaphoreProvider, IActionManagerSemaphoreProvider
{
    /// <inheritdoc/>
    protected override SemaphoreSlim Semaphore { get; } = new (1, 1);
}