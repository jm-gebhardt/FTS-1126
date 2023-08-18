namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Parking;

/// <inheritdoc/>
public class ParkingSemaphoreProvider : IParkingSemaphoreProvider
{
    /// <inheritdoc/>
    public SemaphoreSlim Semaphore { get; } = new (1, 1);
}