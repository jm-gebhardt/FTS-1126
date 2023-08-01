using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <summary>
/// Mutex for assigning of actions to Agvs
/// </summary>
public interface IAgvStateService
{
    /// <summary>
    /// Set AGV busy 
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="wakeFromCharging">If True the Agv will stop charging and go to busy</param>
    /// <param name="ignoreLock">If true the IsLocked of the Agv maybe true</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [JetBrains.Annotations.UsedImplicitly]
    Task SetBusyAsync(IAgv agv, bool wakeFromCharging, bool ignoreLock, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if Agv can be set busy
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="ignoreCharging">If this is set to true, the Agv may also be charging</param>
    /// <param name="ignoreLocked">If this is set to true, the Agv.IsLocked may also be true</param>
    /// <returns>True if the Agv is complete and ready for transition to busy</returns>>
    Task<bool> IsAgvReadyForBusyAsync(Guid agvCorrelationId, CancellationToken cancellationToken, bool ignoreCharging = false, bool ignoreLocked = false);

    /// <summary>
    /// Set agv idle
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SetIdleAsync(Guid agvCorrelationId, CancellationToken cancellationToken);
}