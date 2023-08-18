namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Charging;

/// <inheritdoc/>
public class ChargingSemaphoreProvider : IChargingSemaphoreProvider
{
    /// <inheritdoc/>
    public SemaphoreSlim Semaphore { get; } = new (1, 1);
}