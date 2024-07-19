using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for triggers
/// </summary>
public class TriggerProfile : WebApiProfile
{
    /// <inheritdoc cref="TriggerProfile"/>>
    public TriggerProfile()
    {
        CreateMap<Trigger, TriggerDto>()
            .ForMember(m => m.Name, opt => opt.MapFrom(t => t.DisplayName))
            .ForSourceMember(s => s.Urn, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Version, opt => opt.DoNotValidate());

        CreateMap<ConfigurationParameter, ConfigurationParameterDto>();
        CreateMap<Location, TriggerLocationDto>()
            .ForSourceMember(s => s.TriggerId, opt => opt.DoNotValidate());
        CreateMap<PolygonPoint, PolygonPointDto>()
            .ForSourceMember(s => s.LocationId, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Id, opt => opt.DoNotValidate());
        CreateMap<PolygonPointDto, PolygonPoint>()
            .ForMember(s => s.LocationId, opt => opt.Ignore())
            .ForMember(s => s.Id, opt => opt.Ignore());
    }
}