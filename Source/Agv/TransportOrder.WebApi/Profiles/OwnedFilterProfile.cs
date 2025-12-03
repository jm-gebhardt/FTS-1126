using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Filter;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Filter;

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
    }
}