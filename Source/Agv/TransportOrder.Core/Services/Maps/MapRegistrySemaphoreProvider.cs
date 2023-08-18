namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Maps;

/// <inheritdoc />
public class MapRegistrySemaphoreProvider : IMapRegistrySemaphoreProvider
{
    /// <inheritdoc />
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
}