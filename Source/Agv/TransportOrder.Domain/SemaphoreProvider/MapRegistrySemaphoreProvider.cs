using Gebhardt.StoreWare.Agv.Common.Services;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;

/// <inheritdoc cref="IMapRepositorySemaphoreProvider" />
public class MapRepositorySemaphoreProvider : BaseSemaphoreProvider, IMapRepositorySemaphoreProvider
{
    /// <inheritdoc />
    protected override SemaphoreSlim Semaphore { get; } = new(1, 1);
}