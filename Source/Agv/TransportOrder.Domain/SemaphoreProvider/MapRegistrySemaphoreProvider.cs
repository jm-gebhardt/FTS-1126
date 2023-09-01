using Gebhardt.StoreWare.Agv.Common.Services;

namespace TransportOrder.Domain.SemaphoreProvider;

/// <inheritdoc cref="IMapRegistrySemaphoreProvider" />
public class MapRegistrySemaphoreProvider : BaseSemaphoreProvider, IMapRegistrySemaphoreProvider
{
    /// <inheritdoc />
    protected override SemaphoreSlim Semaphore { get; } = new(1, 1);
}