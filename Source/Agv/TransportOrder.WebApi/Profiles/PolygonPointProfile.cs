using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for polygon points
/// </summary>
public class PolygonPointProfile: WebApiProfile
{
    /// <inheritdoc cref="PolygonPointProfile"/>>
    public PolygonPointProfile()
    {
        CreateMap<TriggerPolygonPoint, PolygonPointDto>()
            .ForSourceMember(s => s.LocationId, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Id, opt => opt.DoNotValidate());
        CreateMap<PolygonPointDto, TriggerPolygonPoint>()
            .ForMember(s => s.LocationId, opt => opt.Ignore())
            .ForMember(s => s.Id, opt => opt.Ignore());

        CreateMap<RestrictedAreaPolygonPoint, PolygonPointDto>()
            .ForSourceMember(s => s.LocationId, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Id, opt => opt.DoNotValidate());
        CreateMap<PolygonPointDto, RestrictedAreaPolygonPoint>()
            .ForMember(s => s.LocationId, opt => opt.Ignore())
            .ForMember(s => s.Id, opt => opt.Ignore());

        CreateMap<PolygonPoint, PolygonPointDto>()
            .ForSourceMember(s => s.LocationId, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Id, opt => opt.DoNotValidate());
        CreateMap<PolygonPointDto, PolygonPoint>()
            .ForMember(s => s.LocationId, opt => opt.Ignore())
            .ForMember(s => s.Id, opt => opt.Ignore());
    }
}