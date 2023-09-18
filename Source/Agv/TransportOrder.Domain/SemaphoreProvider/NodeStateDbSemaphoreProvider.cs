using Gebhardt.StoreWare.Agv.Common.Services;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;

/// <inheritdoc cref="INodeStateDbSemaphoreProvider" />
public class NodeStateDbSemaphoreProvider : BaseSemaphoreProvider, INodeStateDbSemaphoreProvider
{
    /// <inheritdoc />
    protected override SemaphoreSlim Semaphore { get; } = new(1, 1);
}