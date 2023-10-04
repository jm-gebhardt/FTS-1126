using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Response to get the AgvRoute
/// </summary>
/// <param name="AgvRoute"></param>
public record GotAgvRoute(AgvRouteReadModel? AgvRoute);