using Gebhardt.Acc.TransportOrder.Domain.ReadModels;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Response to get the AgvRoute for multiple Agvs
/// </summary>
/// <param name="AgvRoutesByCorrelationId"></param>
public record GotCurrentAgvRoutes(IReadOnlyDictionary<Guid, CurrentAgvRouteReadModel?> AgvRoutesByCorrelationId);