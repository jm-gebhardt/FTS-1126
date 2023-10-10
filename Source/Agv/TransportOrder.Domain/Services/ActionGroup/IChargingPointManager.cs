using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Finds free charger based on Agv Graph and reserves it
/// </summary>
public interface IChargingPointManager
{
    /// <summary>
    /// Selects a free charger based on the agvs graph
    /// </summary>
    /// <param name="agv"></param>
    /// <returns>The best free charger, throws if it could not reserve it</returns>
    Task<GraphNode> ReserveChargerAsync(Models.Agv.Agv agv);

    /// <summary>
    /// Chooses the best free charger based on the agvs graph
    /// </summary>
    /// <param name="agv"></param>
    /// <returns>The best free charger, or null if all occupied</returns>
    Task<GraphNode?> GetBestChargerAsync(Models.Agv.Agv agv);

    /// <summary>
    /// Reserves the given charger
    /// </summary>
    /// <param name="node">The charger to reserve</param>
    /// <param name="agvCorrelationId"></param>
    /// <returns></returns>
    Task ReserveChargerAsync(GraphNode node, Guid agvCorrelationId);
    
    /// <summary>
    /// Checks if the agv is on a charging node
    /// </summary>
    /// <param name="agv"></param>
    /// <returns>True if the lastNodeId of the Agv is a charging point, False if the lastNodeId of the agv is null</returns>
    [UsedImplicitly]
    Task<bool> IsOnAChargingNodeAsync(Models.Agv.Agv agv);
}