using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <summary>
/// More sophisticated logic for dealing with Agvs, like finding Agvs that are ready for being set to busy
/// </summary>
public interface IAgvService
{
    /// <summary>
    /// Returns all Agvs that are ready for being set to busy from the AgvRegistry
    /// </summary>
    /// <param name="ignoreChargingFlag"></param>
    /// <returns></returns>
    Task<IEnumerable<IAgv>> GetReadyAgvsAsync(bool ignoreChargingFlag);

    /// <summary>
    /// Returns all Agvs from the parameter that are ready for being set to busy
    /// </summary>
    /// <param name="agvs"></param>
    /// <param name="ignoreChargingFlag"></param>
    /// <returns></returns>
    Task<IEnumerable<IAgv>> GetReadyAgvsAsync(IEnumerable<IAgv> agvs, bool ignoreChargingFlag);

    /// <summary>
    /// Checks if the given Agv is ready for being set to busy 
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="ignoreChargingFlag"></param>
    /// <returns></returns>
    Task<bool> IsAgvReadyForBusyAsync(IAgv agv, bool ignoreChargingFlag);
}