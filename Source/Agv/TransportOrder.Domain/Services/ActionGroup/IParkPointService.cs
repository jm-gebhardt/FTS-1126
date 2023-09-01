using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Park Point Manager
/// </summary>
public interface IParkPointService
{
    /// <summary>
    /// Reserve parking node async
    /// </summary>
    /// <param name="agv"></param>
    /// <returns></returns>
    Task<GraphNode> ReserveParkAsync(IAgv agv);

    /// <summary>
    /// Checks if the agv is on a park node
    /// </summary>
    /// <param name="agv"></param>
    /// <returns>True if the lastNodeId of the Agv is a park point, False if the lastNodeId of the agv is null</returns>
    Task<bool> IsOnAParkNodeAsync(IAgv agv);
}