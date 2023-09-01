using Gebhardt.StoreWare.Agv.Common.Services;

namespace TransportOrder.Domain.SemaphoreProvider;

/// <inheritdoc cref="IParkingSemaphoreProvider" />
public class ParkingSemaphoreProvider : BaseSemaphoreProvider, IParkingSemaphoreProvider
{
    /// <inheritdoc/>
    protected override SemaphoreSlim Semaphore { get; } = new (1, 1);
}