using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;

/// <summary>
/// Service for providing the AgvRoute, meaning the planned remaining route of an Agv
/// </summary>
public interface IAgvRouteRetrievalService
{
    /// <summary>
    /// Gets the AgvRoute for the given AgvCorrelationId.
    /// If there is no current action for the Agv, null is returned.
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <param name="lastNodeId">You can specify the last node of the Agv, if you leave it blank, it will be retrieved from the system</param>
    /// <returns></returns>
    Task<AgvRouteReadModel?> GetAgvRouteAsync(Guid agvCorrelationId, string? lastNodeId = null);

    /// <summary>
    /// Gets the AgvRoute for the given AgvCorrelationIds
    /// If there is no current action for the Agv, null is returned for this specific Agv.
    /// </summary>
    /// <param name="agvCorrelationIds"></param>
    /// <returns></returns>
    Task<IReadOnlyDictionary<Guid, AgvRouteReadModel?>> GetAgvRouteAsync(ICollection<Guid> agvCorrelationIds);
}