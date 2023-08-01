using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <summary>
/// Checks if IAgv is complete
/// </summary>
public interface IAgvReadyService
{
    /// <inheritdoc cref="IAgvReadyService"/>
    /// <param name="agv"></param>
    /// <param name="ignoreCharging"></param>
    /// <param name="ignoreLocked"></param>
    /// <returns></returns>
    Task<bool> IsAgvCompleteAsync(IAgv agv, bool ignoreCharging = true, bool ignoreLocked = false);
}