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
        CreateMap<Zone, ZoneDto>()
            .ForCtorParam(nameof(ZoneDto.ZoneId), opt => opt.MapFrom(src => src.ZoneName));
        CreateMap<ZoneDto, Zone>()
            .ForMember(z => z.ZoneName, opt => opt.MapFrom(src => src.ZoneId));

        CreateMap<ZonePolygonPoint, ZonePolygonPointDto>().ReverseMap();
    }
}