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
    /// <returns></returns>
    /// <param name="wakeFromCharging">If True the Agv will stop charging and go to busy</param>
    [JetBrains.Annotations.UsedImplicitly]
    Task SetBusyAsync(IAgv agv, bool wakeFromCharging);

    /// <summary>
    /// Checks if Agv can be set busy
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="ignoreCharging">If true the return value will not take the charging flag of the Agv into account</param>
    /// <returns>True if the agv is ready for lock</returns>
    Task<bool> IsAgvReadyForBusyAsync(IAgv agv, bool ignoreCharging = false);

    /// <summary>
    /// Set agv idle
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    Task SetIdleAsync(IAgv agv);
}