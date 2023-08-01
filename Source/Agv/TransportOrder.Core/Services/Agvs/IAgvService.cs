using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <summary>
/// More sophisticated logic for dealing with Agvs, like finding Agvs that are ready for being set to busy
/// </summary>
public interface IAgvService
{
    /// <summary>
    /// Returns the Agv with the given correlationId from the <see cref="AgvStateMachine"/>
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IAgv> GetAgvAsync(Guid? agvCorrelationId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all Agvs that are ready for being set to busy from the AgvRegistry
    /// </summary>
    /// <param name="ignoreChargingFlag"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<IAgv>> GetReadyAgvsAsync(bool ignoreChargingFlag, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all Agvs from the parameter that are ready for being set to busy
    /// </summary>
    /// <param name="agvs"></param>
    /// <param name="ignoreChargingFlag"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<IAgv>> GetReadyAgvsAsync(IEnumerable<IAgv> agvs, bool ignoreChargingFlag, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the given Agv is ready for being set to busy 
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="ignoreCharging">If this is set to true, the Agv may also be charging</param>
    /// <param name="ignoreLocked">If this is set to true, the Agv.IsLocked may also be true</param>
    /// <returns>True if the Agv is complete and ready for transition to busy</returns>>
    Task<bool> IsAgvReadyForBusyAsync(Guid agvCorrelationId, CancellationToken cancellationToken, bool ignoreCharging = false, bool ignoreLocked = false);
}