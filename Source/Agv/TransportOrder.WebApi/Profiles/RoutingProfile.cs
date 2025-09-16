using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Routing;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for routing related objects.
/// </summary>
[UsedImplicitly]
public class RoutingProfile : WebApiProfile
{
    /// <inheritdoc cref="RoutingProfile" />
    public RoutingProfile()
    {
        CreateMap<Route, RouteDto>()
            .ForCtorParam(nameof(RouteDto.Nodes), opt => opt.MapFrom(src => src.Nodes.Select(n => n.Name)));
    }
}