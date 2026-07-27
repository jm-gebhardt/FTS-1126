using Gebhardt.Acc.TransportOrder.Domain.ReadModels;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Routing;

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
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<CurrentAgvRouteReadModel?> GetAgvRouteAsync(Guid agvCorrelationId, Guid? lastNodeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the AgvRoute for the given AgvCorrelationIds
    /// If there is no current action for the Agv, null is returned for this specific Agv.
    /// </summary>
    /// <param name="agvCorrelationIds"></param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<ICollection<CurrentAgvRouteReadModel>> GetAgvRouteAsync(ICollection<Guid> agvCorrelationIds, CancellationToken cancellationToken = default);
}