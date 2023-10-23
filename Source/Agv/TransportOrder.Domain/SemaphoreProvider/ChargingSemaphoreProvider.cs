using Gebhardt.StoreWare.Agv.Common.Services;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;

/// <inheritdoc cref="IChargingSemaphoreProvider"/>
public class ChargingSemaphoreProvider : BaseSemaphoreProvider, IChargingSemaphoreProvider
{
    /// <inheritdoc/>
    protected override SemaphoreSlim Semaphore { get; } = new(1, 1);
}