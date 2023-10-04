using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Response to get the AgvRoute for multiple Agvs
/// </summary>
/// <param name="AgvRoutesByCorrelationId"></param>
public record GotAgvRoutes(IReadOnlyDictionary<Guid, AgvRouteReadModel?> AgvRoutesByCorrelationId);