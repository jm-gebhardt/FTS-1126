using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Filter;
using Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Filter;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterTemplate;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for <see cref="OwnedFilter"/>.
/// </summary>
public class OwnedFilterProfile : WebApiProfile
{
    /// <inheritdoc />
    public OwnedFilterProfile()
    {
        CreateMap<OwnedFilter, OwnedFilterDto>(MemberList.Destination)
            .ForMember(d => d.CorrelationId, o => o.MapFrom(f => f.ResourceCorrelationId))
            .ForMember(d => d.Name, o => o.MapFrom(f => f.ResourceName))
            .ForMember(d => d.Filter, o => o.MapFrom(f => f.Resource));
        CreateMap<OwnedFilterTemplate, FilterTemplateDto>(MemberList.Destination)
            .ForMember(d => d.Id, o => o.MapFrom(f => f.ResourceCorrelationId))
            .ForMember(d => d.Name, o => o.MapFrom(f => f.ResourceName))
            .ForMember(d => d.TimeFilters, o => o.MapFrom(f => f.Resource.TimeFilters))
            .ForMember(d => d.FilterType, o => o.MapFrom(f => f.Resource.FilterType))
            .ForMember(d => d.AgvFilter, o => o.MapFrom(f => f.Resource.AgvFilter))
            .ForMember(d => d.OrderFilter, o => o.MapFrom(f => f.Resource.OrderFilter));
        CreateMap<FilterTemplateDto, FilterTemplate>().ReverseMap();
        CreateMap<AgvFilterTemplateDto, AgvFilterTemplate>().ReverseMap();
        CreateMap<OrderFilterTemplateDto, OrderFilterTemplate>().ReverseMap();
        CreateMap<TimeFilterTemplateDto, TimeFilterTemplate>().ReverseMap();
    }
}