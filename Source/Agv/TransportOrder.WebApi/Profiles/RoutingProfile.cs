using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Routing;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for routing related objects.
/// </summary>
[UsedImplicitly]
public class RoutingProfile : WebApiProfile
{
    /// <inheritdoc cref="RoutingProfile" />
    public RoutingProfile()
    {
        CreateMap<Route, RouteDto>();
    }
}