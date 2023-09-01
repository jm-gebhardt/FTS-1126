using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Interface for charging service
/// </summary>
public interface IChargingService
{
    /// <summary>
    /// Checks if an agv needs to go to the charger or not.
    /// </summary>
    /// <param name="agv"></param>
    /// <returns>true if agv has critical battery level and needs to charge</returns>
    [JetBrains.Annotations.PublicAPI]
    bool AgvNeedsCharging(IAgv agv);

    /// <summary>
    /// Checks if an agv needs to go to the charger or not.
    /// </summary>
    /// <param name="agv"></param>
    /// <returns>true if agv is below battery good level and can charge</returns>
    [JetBrains.Annotations.PublicAPI]
    bool AgvIsEligibleForCharging(IAgv agv);

    /// <summary>
    /// Returns a list of Agvs that are charging but above battery good level
    /// </summary>
    /// <returns>List of Agvs that are charging but above battery good level</returns>
    [JetBrains.Annotations.PublicAPI]
    Task<List<IAgv>> GetChargingAgvsWithGoodBatteryLevel();
}