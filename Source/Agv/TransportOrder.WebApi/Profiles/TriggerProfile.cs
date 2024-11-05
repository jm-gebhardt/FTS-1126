using Gebhardt.Acc.Common.Messages.Triggers;
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
        CreateMap<ConfigurationParameterDto, ConfigurationParameter>()
            .ForSourceMember(s => s.Description, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Validation, opt => opt.DoNotValidate());

        CreateMap<Location, TriggerLocationDto>()
            .ForSourceMember(s => s.TriggerId, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Agvs, opt => opt.DoNotValidate());
        CreateMap<TriggerLocationDto,Location>()
            .ForMember(d => d.TriggerId, opt => opt.Ignore())
            .ForMember(d => d.Agvs, opt => opt.Ignore());
        CreateMap<LocationDto, Location>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.TriggerId, opt => opt.Ignore())
            .ForMember(d => d.Agvs, opt => opt.Ignore());
    }
}