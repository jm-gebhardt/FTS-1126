using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;

/// <summary>
/// Finds free charger based on Agv Graph and reserves it
/// </summary>
public interface IChargingPointManager
{
    /// <summary>
    /// Selects a free charger based on the agvs graph
    /// </summary>
    /// <param name="agvGraph"></param>
    /// <returns>The best free charger, throws if it could not reserve it</returns>
    public AgvGraph.Node ReserveBestCharger(AgvGraph agvGraph);
	
    /// <summary>
    /// Chooses the best free charger based on the agvs graph
    /// </summary>
    /// <param name="agvGraph"></param>
    /// <returns>The best free charger, or null if all occupied</returns>
    public AgvGraph.Node? GetBestCharger(AgvGraph agvGraph);
	
    /// <summary>
    /// Reserves the given charger
    /// </summary>
    /// <param name="node">The charger to reserve</param>
    /// <returns>True if it could be reserved</returns>
    public bool ReserveCharger(AgvGraph.Node node);
}