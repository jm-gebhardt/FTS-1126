using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Zone;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for <see cref="Zone"/>.
/// </summary>
public class ZoneProfile : WebApiProfile
{
    /// <inheritdoc cref="ZoneProfile"/>
    public ZoneProfile()
    {
        CreateMap<Zone, ZoneDto>().ReverseMap();
        CreateMap<ZonePolygonPoint, ZonePolygonPointDto>().ReverseMap();
    }
}