namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <summary>
/// Tells us whether an Agv is loaded or can get more load
/// </summary>
public interface IAgvLoadCapabilityService
{
    /// <summary>
    /// Checks if Agv is carrying load or not
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <returns>True if it carrying any load</returns>
    public bool IsCarryingLoad(Guid agvCorrelationId);


    /// <summary>
    /// Checks if Agv can get new load
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <returns>True if it can take a new load</returns>
    public bool CanGetNewLoad(Guid agvCorrelationId);
}