using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

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
    /// Reserve specific parking node. 
    /// </summary>
    /// <param name="parkNodeId"></param>
    /// <param name="agvCorrelationId"></param>
    /// <returns>Throws if reservations fails.</returns>
    Task ReserveParkAsync(Guid parkNodeId, Guid agvCorrelationId);

    /// <summary>
    /// Checks if the agv is on a park node
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <returns>True if the lastNodeId of the Agv is a park point, False if the lastNodeId of the agv is null</returns>
    Task<bool> IsOnAParkNodeAsync(Models.Agv.Agv agv);

    /// <summary>
    /// Checks if the agv has already reserved a parking node.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <returns>True if a parking node has been reserved previously for this agv.</returns>
    Task<bool> AgvHasReservedParkingNodeAsync(Guid agvCorrelationId);

    /// <summary>
    /// Checks if there are any parkers available for the given agv.
    /// </summary>
    /// <param name="agvCorrelationId">The Agv correlation Id to check.</param>
    /// <returns>True if there are basically any parkers</returns>
    Task<bool> HasParkersAsync(Guid agvCorrelationId);
}