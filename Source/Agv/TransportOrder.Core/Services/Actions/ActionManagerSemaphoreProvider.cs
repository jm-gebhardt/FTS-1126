namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <inheritdoc/>
public class ActionManagerSemaphoreProvider : IActionManagerSemaphoreProvider
{
    /// <inheritdoc/>
    public SemaphoreSlim Semaphore { get; } = new (1, 1);
}