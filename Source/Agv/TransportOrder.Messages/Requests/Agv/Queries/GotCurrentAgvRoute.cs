using Gebhardt.Acc.TransportOrder.Domain.ReadModels;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Response to get the AgvRoute
/// </summary>
/// <param name="AgvRoute"></param>
public record GotCurrentAgvRoute(CurrentAgvRouteReadModel? AgvRoute);