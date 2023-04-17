namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;

/// <summary>
/// Tells us whether an Agv is loaded or can get more load
/// </summary>
public interface IAgvLoadCapabilityService
{
    /// <summary>
    /// Checks if Agv is carrying load or not
    /// </summary>
    /// <param name="agvId"></param>
    /// <returns>True if it carrying any load</returns>
    public bool IsCarryingLoad(string agvId);


    /// <summary>
    /// Checks if Agv can get new load
    /// </summary>
    /// <param name="agvId"></param>
    /// <returns>True if it can take a new load</returns>
    public bool CanGetNewLoad(string agvId);
}