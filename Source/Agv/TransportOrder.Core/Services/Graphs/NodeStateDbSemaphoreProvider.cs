namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;

/// <inheritdoc />
public class NodeStateDbSemaphoreProvider : INodeStateDbSemaphoreProvider
{
    /// <inheritdoc />
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
}