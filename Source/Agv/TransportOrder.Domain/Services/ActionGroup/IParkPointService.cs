using Gebhardt.StoreWare.Agv.Common.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Park Point Manager
/// </summary>
public interface IParkPointService
{
    /// <summary>
    /// Reserve parking node async
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <returns></returns>
    Task<GraphNode> ReserveParkAsync(Models.Agv.Agv agv);

    /// <summary>
    /// Checks if the agv is on a park node
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <returns>True if the lastNodeId of the Agv is a park point, False if the lastNodeId of the agv is null</returns>
    Task<bool> IsOnAParkNodeAsync(Models.Agv.Agv agv);
}