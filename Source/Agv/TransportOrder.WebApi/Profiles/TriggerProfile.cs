using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Trigger;
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
            .ForMember(d => d.NodeId, opt => opt.MapFrom(s => s.NodeId != null ? s.Node!.Name : null))
            .ForSourceMember(s => s.TriggerId, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Agvs, opt => opt.DoNotValidate());
        CreateMap<TriggerLocationDto, Location>()
            .ForMember(d => d.NodeId, opt => opt.MapFrom(s => s.NodeId != null ? (Guid?)s.NodeId.ToGuid() : null))
            .ForMember(d => d.Node, opt => opt.MapFrom(s => s.NodeId != null ? s.NodeId.ToGraphNode() : null))
            .ForMember(d => d.TriggerId, opt => opt.Ignore())
            .ForMember(d => d.Agvs, opt => opt.Ignore());

        CreateMap<TriggerLocationAssigned, TriggerLocationDto>()
            .IncludeMembers(s => s.Location)
            .ForMember(d => d.NodeId, opt => opt.MapFrom(s => s.Node != null ? s.Node.Name : null));

        CreateMap<LocationDto, Location>()
            .ConstructUsing(_ => new Location())
            .ForMember(d => d.Node, opt => opt.MapFrom(s => s.NodeId != null ? s.NodeId.ToGraphNode() : null))
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.TriggerId, opt => opt.Ignore())
            .ForMember(d => d.Agvs, opt => opt.Ignore());
    }
}