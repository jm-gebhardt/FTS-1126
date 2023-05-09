using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions.Parking;

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
    Task<AgvGraph.Node> ReserveParkAsync(IAgv agv);

    /// <summary>
    /// Checks if the agv is on a park node
    /// </summary>
    /// <param name="agv"></param>
    /// <returns>True if the lastNodeId of the Agv is a park point, False if the lastNodeId of the agv is null</returns>
    Task<bool> IsOnAParkNodeAsync(IAgv agv);
}