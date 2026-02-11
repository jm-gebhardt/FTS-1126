using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Finds free charger based on Agv Graph and reserves it
/// </summary>
public interface IChargingPointManager
{
    /// <summary>
    /// Selects a free charger based on the agvs graph
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="startNodeId">The start node Id from which the route should be calculated. If null the last node Id of the agv will be used.</param>
    /// <param name="ignoreLocked">Flag to reserve charger even if it is locked.</param>
    /// <returns>The best free charger, throws if it could not reserve it</returns>
    Task<GraphNode> ReserveChargerAsync(Agv agv, Guid? startNodeId = null, bool ignoreLocked = false);

    /// <summary>
    /// Chooses the best free charger based on the agvs graph
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="startNodeId">The start node Id from which the route should be calculated. If null the last node Id of the agv will be used.</param>
    /// <returns>The best free charger, or null if all occupied</returns>
    Task<GraphNode?> GetBestChargerAsync(Agv agv, Guid? startNodeId = null);

    /// <summary>
    /// Reserves the given charger
    /// </summary>
    /// <param name="node">The charger to reserve</param>
    /// <param name="agvCorrelationId"></param>
    /// <param name="ignoreLocked">Flag to reserve charger even if it is locked.</param>
    /// <returns></returns>
    Task ReserveChargerAsync(GraphNode node, Guid agvCorrelationId, bool ignoreLocked = false);

    /// <summary>
    /// Checks if the agv is on a charging node
    /// </summary>
    /// <param name="agv"></param>
    /// <returns>True if the lastNodeId of the Agv is a charging point, False if the lastNodeId of the agv is null</returns>
    [UsedImplicitly]
    Task<bool> IsOnAChargingNodeAsync(Agv agv);

    /// <summary>
    /// Checks if the agv has already reserved a charger.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <returns>True if a charger has been reserved previously for this agv.</returns>
    Task<bool> AgvHasReservedChargerAsync(Guid agvCorrelationId);

    /// <summary>
    /// Gets NodeLock currently reserved for charging for an AGV
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <returns></returns>
    Task<GraphNode?> GetReservedChargerForAgvAsync(Guid agvCorrelationId);
}